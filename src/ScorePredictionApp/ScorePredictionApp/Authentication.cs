namespace ScorePredictionApp
{
    public record Authentication
    {
        public Google Google { get; set; }
    }

    public record Google
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
