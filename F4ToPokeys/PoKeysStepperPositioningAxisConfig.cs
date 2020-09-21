namespace F4ToPokeys
{
    public class PoKeysStepperPositioningAxisConfig
    {
        public bool Inverted { get; set; } = false;
        public bool HomeInverted { get; set; } = false;
        public int HomingSpeed { get; set; } = 50;
        public int HomingReturnSpeed { get; set; } = 20;
        public bool HasHomeSwitch { get; set; } = true;
        public int PinHomeSwitch { get; set; } = 0;

        public float MaxSpeed { get; set; } = 10;
        public float MaxAcceleration { get; set; } = 0.01f;
        public float MaxDecceleration { get; set; } = 0.01f;
        public float UpdateIntervalMs { get; set; } = 500;
        public int MaxMovementTolerance { get; set; } = 10;

        public int SoftLimitMaximum { get; set; } = 3780;
        public int SoftLimitMinimum { get; set; } = 0;

        public double StepsPerUnit { get; set; } = 25;

        public int MPGjogEncoder { get; set; } = 0;
        public int MPGjogMultiplier { get; set; } = 1;
    }
}
