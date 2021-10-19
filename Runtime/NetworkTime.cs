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
        ///* Retorna o cronômetro.
        /// </summary>
        public Stopwatch Stopwatch => _stopwatch;

        /// <summary>
        ///* Retorna a quantidade de pacotes que falharam em porcentagem(%).
        /// </summary>
        /// <value></value>
        public double PacketLoss => Math.Abs(Math.Round(100d - ((Rpu / Spu) * 100d)));

        /// <summary>
        ///* Retorna o atraso em milissegundos (ms) que uma solitação de rede leva para chegar ao seu destino.
        /// </summary>
        /// <value></value>
        public double Latency => Math.Round((RoundTripTime * 0.5d) * 1000d);

        /// <summary>
        ///* Retorna a duração em segundos (sec) que uma solicitação de rede leva para ir de um ponto de partida a um destino e voltar.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).
        /// </summary>
        /// <value></value>
        public double RoundTripTime => _rttExAvg.Avg;

        /// <summary>
        ///* Obtenha o tempo atual em segundos(sec) desde do início da conexão.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).<br/>
        ///* Não afetado pela rede.
        /// </summary>
        public double LocalTime => Stopwatch.Elapsed.TotalSeconds;

        /// <summary>
        ///* Obtenha o tempo atual da rede em segundos(sec).<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).
        /// </summary>
        public double Time => LocalTime + (Offset * -1);

        /// <summary>
        ///* A variação do tempo de ida e volta(rtt), quanto maior menos preciso é.
        /// </summary>
        public double RttSlope => _rttExAvg.Slope;

        /// <summary>
        ///* A variação da diferença de tempo, quanto maior menos preciso é.
        /// </summary>
        public double OffsetSlope => _offsetExAvg.Slope;

        /// <summary>
        ///* A diferença de tempo entre o cliente e o servidor.
        /// </summary>
        public double Offset => _offsetExAvg.Avg;

        public void GetNetworkTime(double clientTime, double serverTime)
        {
            //* Obtém o tempo atual do cliente.
            double now = LocalTime;

            //* Obtém o tempo de ida e volta(rtt) de uma solicitação de rede.
            double rtt = now - clientTime;
            _rttExAvg.Increment(rtt);

            //* A latência da rede, isto é o mais próximo que temos, então sempre estaremos atrasados alguns 0.000ms....
            double halfRtt = rtt * 0.5d;
            //* A diferença do tempo entre o cliente e o servidor.
            double offset = now - halfRtt - serverTime;

            double offsetMin = now - rtt - serverTime;
            double offsetMax = now - serverTime;
            //* Mantém a diferença entre os limites de variação do rtt.
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