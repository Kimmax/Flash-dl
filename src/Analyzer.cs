using System;
using System.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace Flash_dl
{
    public enum SensivityLevel
    {
        VERY_LOW = 120,
        LOW = 110,
        NORMAL = 100,
        HIGH = 80,
        VERY_HIGH = 70
    };

    public sealed class SpectrumBeatDetector
    {
        #region Fields

        // Constants
        private const int BANDS = 10;

        // Events
        public delegate void BeatDetectedHandler(byte Value);

        // Threading
        private Thread _AnalysisThread;

        // BASS Process
        private WASAPIPROC _WasapiProcess = new WASAPIPROC(SpectrumBeatDetector.Process);

        // Analysis settings
        private int _SamplingRate;
        private int _DeviceCode;
        private SensivityLevel _BASSSensivity;
        private SensivityLevel _MIDSSensivity;

        // Analysis data
        private float[] _FFTData = new float[4096];

        #endregion

        #region Setup methods

        public SpectrumBeatDetector(int DeviceCode, int SamplingRate = 44100, SensivityLevel BASSSensivity = SensivityLevel.NORMAL, SensivityLevel MIDSSensivity = SensivityLevel.NORMAL)
        {
            _SamplingRate = SamplingRate;
            _BASSSensivity = BASSSensivity;
            _MIDSSensivity = MIDSSensivity;
            _DeviceCode = DeviceCode;
            Init();
        }

        // BASS initialization method
        private void Init()
        {
            bool result = false;

            // Initialize BASS on default device
            result = Bass.BASS_Init(0, _SamplingRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            if (!result)
            {
                throw new Exception(Bass.BASS_ErrorGetCode().ToString());
            }

            BASS_WASAPI_DEVICEINFO[] devices = BassWasapi.BASS_WASAPI_GetDeviceInfos();
            
            // 9 = Main, 11 = Headset
            // Initialize WASAPI
            result = BassWasapi.BASS_WASAPI_Init(3, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _WasapiProcess, IntPtr.Zero);

            if (!result)
            {
                result = BassWasapi.BASS_WASAPI_Init(7, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _WasapiProcess, IntPtr.Zero);

                if (!result)
                {
                    result = BassWasapi.BASS_WASAPI_Init(11, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _WasapiProcess, IntPtr.Zero);
                    if (!result)
                        throw new Exception(Bass.BASS_ErrorGetCode().ToString());
                }
            }

            BassWasapi.BASS_WASAPI_Start();
            System.Threading.Thread.Sleep(500);
        }


        ~SpectrumBeatDetector()
        {
            // Kill working thread and clean after BASS
            if (_AnalysisThread != null && _AnalysisThread.IsAlive)
            {
                _AnalysisThread.Abort();
            }

            Free();
        }

        // Sensivity Setters
        public void SetBassSensivity(SensivityLevel Sensivity)
        {
            _BASSSensivity = Sensivity;
        }

        public void SetMidsSensivity(SensivityLevel Sensivity)
        {
            _MIDSSensivity = Sensivity;
        }

        #endregion

        #region BASS-dedicated Methods

        // WASAPI callback, required for continuous recording
        private static int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        // Cleans after BASS
        public void Free()
        {
            BassWasapi.BASS_WASAPI_Free();
            Bass.BASS_Free();
        }

        #endregion

        #region Analysis public methods

        // Starts a new Analysis Thread
        public void StartAnalysis()
        {
            // Kills currently running analysis thread if alive
            if (_AnalysisThread != null && _AnalysisThread.IsAlive)
            {
                _AnalysisThread.Abort();
            }

            // Starts a new high-priority thread
            _AnalysisThread = new Thread(delegate()
            {
                while (true)
                {
                    //Stopwatch SW = new Stopwatch();
                    //SW.Start();
                    Thread.Sleep(5);
                    PerformAnalysis();
                    //SW.Stop();
                    //Console.WriteLine(SW.Elapsed);
                }
            });

            _AnalysisThread.Priority = ThreadPriority.Highest;
            _AnalysisThread.Start();
        }

        // Kills running thread
        public void StopAnalysis()
        {
            if (_AnalysisThread != null && _AnalysisThread.IsAlive)
            {
                _AnalysisThread.Abort();
            }
        }

        #endregion

        #region Analysis private methods

        // Performs FFT analysis in order to detect beat
        private void PerformAnalysis()
        {
            // Specifes on which result end which band (dividing it into 10 bands)
            // 19 - bass, 187 - mids, rest is highs
            int[] BandRange = { 4, 8, 18, 38, 48, 94, 140, 186, 466, 1022, 22000 };
            double[] BandsTemp = new double[BANDS];
            int n = 0;
            int level = BassWasapi.BASS_WASAPI_GetLevel();

            // Get FFT
            int ret = BassWasapi.BASS_WASAPI_GetData(_FFTData, (int)BASSData.BASS_DATA_FFT1024 | (int)BASSData.BASS_DATA_FFT_COMPLEX); //get channel fft data
            if (ret < -1) return;

            // Calculate the energy of every result and divide it into subbands
            float sum = 0;

            for (int i = 2; i < 2048; i = i + 2)
            {
                float real = _FFTData[i];
                float complex = _FFTData[i + 1];
                sum += (float)Math.Sqrt((double)(real * real + complex * complex));

                if (i == BandRange[n])
                {
                    BandsTemp[n++] = (BANDS * sum) / 1024;
                    sum = 0;
                }
            }

            int[] drawnBandHeights = new int[BandsTemp.Length];
            const int MAX_DRAW_HEIGHT = 25;

            for (int i = 0; i < BandsTemp.Length; i++)
            {
                drawnBandHeights[i] = (int)(BandsTemp[i] * 100000);
            }

            int LastTop = Console.CursorTop;
            int LastLeft = Console.CursorLeft;

            for(int currentY = 0; currentY < MAX_DRAW_HEIGHT; currentY++)
            {
                for(int currentX = 0; currentX < BandsTemp.Length; currentX++)
                {
                    if (drawnBandHeights[currentX] >= currentY)
                    {
                        Console.SetCursorPosition(currentX, Console.WindowHeight - currentY);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("^");

                        Console.SetCursorPosition(Console.WindowWidth - 1 - currentX, Console.WindowHeight - currentY);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("^");
                    }
                    else
                    {
                        Console.SetCursorPosition(currentX, Console.WindowHeight - currentY);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write(" ");

                        Console.SetCursorPosition(Console.WindowWidth - 1 - currentX, Console.WindowHeight - currentY);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write(" ");
                    }
                }
            }

            Console.CursorTop = LastTop;
            Console.CursorLeft = LastLeft;
        }
        #endregion
    }
}