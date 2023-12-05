public interface IAuthorRepository
{
	public AuthorDTO? GetAuthorByName(string name);
	public AuthorDTO? GetAuthorByEmail(string email);
	public void CreateNewAuthor(string name, string email);
	public Task FollowAuthor(string followerName, string followeeName);
	public Task UnfollowAuthor(string followerName, string followeeName);
	public List<AuthorDTO> GetFollowing(string authorname);
}