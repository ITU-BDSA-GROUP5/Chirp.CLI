namespace Chirp.CLI 
{
	static class UserInterface
	{	
        public static void PrintCheeps(IEnumerable<Cheep> cheeps)
		{
			foreach (Cheep cheep in cheeps) 
			{
				Console.WriteLine(FormatCheep(cheep));
			}
		}
        
		public static string FormatCheep(Cheep cheep)
		{
			DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp).LocalDateTime;

			return $"{cheep.Author} @ {timestamp.ToString("G")}: {cheep.Message}";
		}
	}

}
