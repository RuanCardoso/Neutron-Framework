using NeutronNetwork.Internal;
using System;
using System.Diagnostics;

namespace NeutronNetwork.Client.Internal
{
    public class NetworkTime
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        public double Rpu = 1d;
        public double Spu = 1d;
        private double _offsetMin = double.MinValue;
        private double _offsetMax = double.MaxValue;
        private const int Size = 10;
        private readonly ExponentialAvg _rttExAvg = new ExponentialAvg(Size);
        private readonly ExponentialAvg _offsetExAvg = new ExponentialAvg(Size);
        /// <summary>
        ///* Retorna o cron�metro.
        /// </summary>
        public Stopwatch Stopwatch => _stopwatch;

        /// <summary>
        ///* Retorna a quantidade de pacotes que falharam em porcentagem(%).
        /// </summary>
        /// <value></value>
        public double PacketLoss => Math.Abs(Math.Round(100d - ((Rpu / Spu) * 100d)));

        /// <summary>
        ///* Retorna o atraso em milissegundos (ms) que uma solita��o de rede leva para chegar ao seu destino.
        /// </summary>
        /// <value></value>
        public double Latency => Math.Round((RoundTripTime * 0.5d) * 1000d);

        /// <summary>
        ///* Retorna a dura��o em segundos (sec) que uma solicita��o de rede leva para ir de um ponto de partida a um destino e voltar.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).
        /// </summary>
        /// <value></value>
        public double RoundTripTime => _rttExAvg.Avg;

        /// <summary>
        ///* Obtenha o tempo atual em segundos(sec) desde do in�cio da conex�o.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).<br/>
        ///* N�o afetado pela rede.
        /// </summary>
        public double LocalTime => Stopwatch.Elapsed.TotalSeconds;

        /// <summary>
        ///* Obtenha o tempo atual da rede em segundos(sec).<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).
        /// </summary>
        public double Time => LocalTime + (Offset * -1);

        /// <summary>
        ///* A varia��o do tempo de ida e volta(rtt), quanto maior menos preciso �.
        /// </summary>
        public double RttSlope => _rttExAvg.Slope;

        /// <summary>
        ///* A varia��o da diferen�a de tempo, quanto maior menos preciso �.
        /// </summary>
        public double OffsetSlope => _offsetExAvg.Slope;

        /// <summary>
        ///* A diferen�a de tempo entre o cliente e o servidor.
        /// </summary>
        public double Offset => _offsetExAvg.Avg;

        public void GetNetworkTime(double clientTime, double serverTime)
        {
            //* Obt�m o tempo atual do cliente.
            double now = LocalTime;

            //* Obt�m o tempo de ida e volta(rtt) de uma solicita��o de rede.
            double rtt = now - clientTime;
            _rttExAvg.Increment(rtt);

            //* A lat�ncia da rede, isto � o mais pr�ximo que temos, ent�o sempre estaremos atrasados alguns 0.000ms....
            double halfRtt = rtt * 0.5d;
            //* A diferen�a do tempo entre o cliente e o servidor.
            double offset = now - halfRtt - serverTime;

            double offsetMin = now - rtt - serverTime;
            double offsetMax = now - serverTime;
            //* Mant�m a diferen�a entre os limites de varia��o do rtt.
            _offsetMin = Math.Max(_offsetMin, offsetMin);
            _offsetMax = Math.Min(_offsetMax, offsetMax);

            if (_offsetExAvg.Avg < _offsetMin || _offsetExAvg.Avg > _offsetMax)
            {
                _offsetExAvg.Reset(Size);
                _offsetExAvg.Increment(offset);
            }
            else if (offset >= _offsetMin || offset <= _offsetMax)
                _offsetExAvg.Increment(offset);
        }
    }
}