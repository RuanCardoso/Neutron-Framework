namespace NeutronNetwork.Internal
{
    public class ExponentialAvg
    {
        private bool _initialized;
        private double _alpha;

        public double Avg {
            get;
            private set;
        }
        public double Slope {
            get;
            private set;
        }

        public ExponentialAvg(int size) => _alpha = 2.0d / (size + 1);

        public void Increment(double value)
        {
            if (_initialized)
            {
                double delta = value - Avg;
                Avg += _alpha * delta;
                Slope = (1 - _alpha) * (Slope + _alpha * delta * delta);
            }
            else
            {
                if (!_initialized)
                {
                    Avg = value;
                    _initialized = true;
                }
                else
                    LogHelper.Error("Avg has been initialized!");
            }
        }

        public void Reset(int size)
        {
            if (Avg > 0)
            {
                if (_initialized)
                {
                    _initialized = false;
                    _alpha = 2.0d / (size + 1);
                }
                else
                    LogHelper.Error("Avg not initialized!");
            }
        }
    }
}