namespace LifeTgBotChecker.Data
{
    public record class Bot
    {
        private bool _isLife;
        public bool IsLife
        {
            get { return _isLife; }
            set { _isLife = value; Color = value ? "Green" : "Red"; }
        }
        public string Token;
        public string Name;
        public int Workload { get; set; }
        public string Color { get; set; } = "White";
        public Bot(string token, string name)
        {
            Token = token;
            Name = name;
        }
    }
}
