namespace LifeTgBotChecker.Data
{
    public record class Bot(string Token)
    {
        private bool _isLife;
        public bool IsLife
        {
            get { return _isLife; }
            set { _isLife = value; Color = value ? "Green" : "Red"; }
        }
        public int Workload { get; set; }
        public string Color { get; set; } = "White";
    }
}
