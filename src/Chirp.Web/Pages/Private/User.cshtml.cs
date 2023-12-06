using Chirp.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace MyApp.Namespace
{
    public class UserModel : PageModel
    {
        private readonly IAuthorRepository AuthorRepository;
        private readonly ICheepRepository CheepRepository;
        public required List<CheepDTO> Cheeps { get; set; }
        public required List<AuthorDTO> Followees { get; set; }

        public int PageNumber { get; set; }
        public int LastPageNumber { get; set; }
        public string? PageUrl { get; set; }

        public string? Mail { get; set; }


        public UserModel(IAuthorRepository authorRepository, ICheepRepository cheepRepository)
        {
            AuthorRepository = authorRepository;
            CheepRepository = cheepRepository;
        }

        public async Task<ActionResult> OnGet([FromQuery(Name = "page")] int page = 1)
        {
            string? name = User.Identity?.Name;

            if (name != null)
            {
                Cheeps = CheepRepository.GetCheepsFromAuthor(page, name);
                PageNumber = page;
                LastPageNumber = CheepRepository.GetPageAmount(name);
                Followees = AuthorRepository.GetFollowing(name);
                PageUrl = HttpContext.Request.GetEncodedUrl().Split("?")[0];

                string token = User.FindFirst("idp_access_token")?.Value
                    ?? throw new Exception("Github token not found");

                Mail = await GithubHelper.GetUserEmailGithub(token, name);
            }

            return Page();
        }

        //This deletes the user and associated cheeps from the database. Note that it does not log out or change anything azure and cookie related
        public ActionResult OnPostDelete()
        {
            AuthorRepository.DeleteAuthorByName(User.Identity?.Name!);
            return Redirect("/");
        }
    }
}
