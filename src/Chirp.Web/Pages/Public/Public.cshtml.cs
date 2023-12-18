﻿﻿using Chirp.Core;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PublicModel : PageModel
{
	private readonly IAuthorRepository AuthorRepository;
	private readonly ICheepRepository CheepRepository;
	private readonly IValidator<CreateCheepDTO> CheepValidator;
	public required List<CheepDTO> Cheeps { get; set; }

	public required List<AuthorDTO> Following { get; set; }

	[BindProperty]
	public string? CheepMessage { get; set; }

	public bool InvalidCheep { get; set; }
	public string? ErrorMessage { get; set; }
	public int PageNumber { get; set; }
	public int LastPageNumber { get; set; }

	[BindProperty]
	public string? ReturnUrl { get; set; }

	public PublicModel(IAuthorRepository authorRepository, ICheepRepository cheepRepository, IValidator<CreateCheepDTO> _cheepValidator)
	{
		AuthorRepository = authorRepository;
		CheepRepository = cheepRepository;
		CheepValidator = _cheepValidator;
	}

	private async Task EnsureAuthorCreated()
	{
		// If user is not authenticated, just return
		if (User.Identity != null && !User.Identity.IsAuthenticated)
		{
			return;
		}

		string authorCookieName = "AuthorCreated";

		// If cookie exists, return
		string? authorCookie = Request.Cookies[authorCookieName];
		
		if (authorCookie != null)
		{
			return;
		}

		var authorName = User.Identity?.Name
			?? throw new Exception("User identity name is null");

        var author = AuthorRepository.GetAuthorByName(authorName);

		if (author == null)
		{
			string token = User.FindFirst("idp_access_token")?.Value
				?? throw new Exception("Github token not found");

			string email = await GithubHelper.GetUserEmailGithub(token, authorName);

			try
			{
				AuthorRepository.CreateNewAuthor(authorName, email);
			}
			catch (Exception e)
			{
				Console.WriteLine($"User creation failed: {e}");
				return;
			}
		}
		
		Response.Cookies.Append(authorCookieName, true.ToString());
	}

	public ActionResult OnGet([FromQuery(Name = "page")] int page = 1)
	{
		if (User.Identity != null && User.Identity.IsAuthenticated)
		{
			EnsureAuthorCreated().Wait();
			string name = (User.Identity?.Name) ?? throw new Exception("Name is null!");
			Following = AuthorRepository.GetFollowing(name);
		}

		Cheeps = CheepRepository.GetCheeps(page);
		PageNumber = page;
		LastPageNumber = CheepRepository.GetPageAmount();

		return Page();
	}
	public IActionResult OnPost()
	{
		InvalidCheep = false;
		try
		{
			if (CheepMessage == null)
			{
				throw new Exception("Cheep is empty!");
			}

			string name = (User.Identity?.Name) ?? throw new Exception("Error in getting username");
			AuthorDTO? user = AuthorRepository.GetAuthorByName(name)
				?? throw new Exception("User not found!");

            CreateCheepDTO cheep = new CreateCheepDTO()
			{
				Text = CheepMessage,
				Name = user.Name,
				Email = user.Email
			};

			CheepValidator.ValidateAndThrow(cheep);

			CheepRepository.CreateNewCheep(cheep);
			CheepMessage = null;
		}
		catch (Exception e)
		{
			ErrorMessage = e.Message;
			InvalidCheep = true;
		}

		return OnGet();
	}

	public IActionResult OnPostLike(Guid cheep)
	{
		if (User.Identity == null || !User.Identity.IsAuthenticated || User.Identity.Name == null)
		{
			return Unauthorized();
		}

		try
		{
			CheepRepository.LikeCheep(cheep, User.Identity.Name);
		}
		catch (Exception e)
		{
			ErrorMessage = e.Message;
		}

		return Redirect(ReturnUrl ?? "/");
	}

	public IActionResult OnPostUnlike(Guid cheep)
	{
		if (User.Identity == null || !User.Identity.IsAuthenticated || User.Identity.Name == null)
		{
			return Unauthorized();
		}

		try
		{
			CheepRepository.UnlikeCheep(cheep, User.Identity.Name);
		}
		catch (Exception e)
		{
			ErrorMessage = e.Message;
		}

		return Redirect(ReturnUrl ?? "/");
	}

	public IActionResult OnPostFollow(string followeeName, string followerName)
	{
		AuthorRepository.FollowAuthor(followerName ?? throw new Exception("Name is null!"), followeeName);
		return Redirect(ReturnUrl ?? "/");
	}

	public IActionResult OnPostUnfollow(string followeeName, string followerName)
	{
		AuthorRepository.UnfollowAuthor(followerName ?? throw new Exception("Name is null!"), followeeName);
		return Redirect(ReturnUrl ?? "/");
	}

	public IActionResult OnPostDeleteCheep(Guid id)
	{
		CheepRepository.DeleteCheep(id);
		return RedirectToPage("Public");
	}
}
