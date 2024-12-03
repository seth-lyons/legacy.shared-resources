namespace SharedResources
{
    public static class EmailBuilder
    {
        public static string EmailButton(string text, string link, string color = "#23a7e0")
        {
            return Operations.RenderFromTemplate("EmailButton.html", new { Link = link, Text = text, Color = color }, useExecutingAssembly: true);
        }
    }
}
