namespace F4ToPokeys
{
    public class PoKeysStepperBoardConfiguration
    {
        public PoKeysStepperPositioningAxisConfig[] AxisConfig { get; }
        public bool InvertEmergencySwitchPolarity { get; set; } = false;

        public PoKeysStepperBoardConfiguration()
        {
            AxisConfig = new PoKeysStepperPositioningAxisConfig[8];

            for (int i = 0; i < 8; i++)
                AxisConfig[i] = new PoKeysStepperPositioningAxisConfig() { MaxSpeed = 15 };
        }
    }
}
