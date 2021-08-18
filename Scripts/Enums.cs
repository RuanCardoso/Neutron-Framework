using NeutronNetwork.Internal.Attributes;
using System;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>

namespace NeutronNetwork.Packets
{
    /// <summary>
    ///* Define pacotes personalizados para extender o Neutron com funcionalidades proprias.
    /// </summary>
    [Network]
    public enum CustomPacket : byte
    {
        [Obsolete("Do not use this packet, it is only for testing (:", true)] CustomTest,
    }

    /// <summary>
    ///* Define os alvos de recepção do pacote.
    /// </summary>
    [Network]
    public enum TargetTo : byte
    {
        /// <summary>
        ///* Inclui todos os jogadores na lista de recepção do pacote.
        /// </summary>
        All,
        /// <summary>
        ///* Inclui somente você na lista de recepção do pacote.
        /// </summary>
        Me,
        /// <summary>
        ///* Inclui todos os jogadores na lista de recepção do pacote, exceto você.
        /// </summary>
        Others,
        /// <summary>
        ///* Inclui somente o servidor na lista de recepção do pacote.
        /// </summary>
        Server,
    }

    /// <summary>
    ///* Define o tipo de Matchmaking que será usado para transmitir ou receber alguns pacotes.
    /// </summary>
    [Network]
    public enum MatchmakingMode : byte
    {
        Room,
        Channel,
        Group
    }

    /// <summary>
    ///* Define o canal que será usado para transmitir as mensagens.
    /// </summary>
    [Network]
    public enum ChatMode : byte
    {
        /// <summary>
        ///* Envia uma mensagem de chat global, isto inclui todos os canais, salas e grupos.
        /// </summary>
        Global,
        /// <summary>
        ///* Envia uma mensagem de chat para um jogador específico.
        /// </summary>
        Private,
    }

    [Network]
    public enum CachedPacket : byte
    {
        gRPC = 255,
        iRPC = 254,
        Custom = 253,
    }

    /// <summary>
    ///* Define onde os dados devem ser tunelados.
    /// </summary>
    [Network]
    public enum TunnelingTo : byte
    {
        /// <summary>
        ///* Used with <see cref="TargetTo.Me"></see>
        /// </summary>
        Me,
        /// <summary>
        ///* Tunela os dados no servidor, isto inclui todos os canais, salas e grupos.
        /// </summary>
        Server,
        /// <summary>
        ///* Tunela os dados no canal, isto inclui as salas e grupos que pertencem ao canal.
        /// </summary>
        Channel,
        /// <summary>
        ///* Tunela os dados na sala, isto inclui os grupos que pertencem a sala.
        /// </summary>
        Room,
        /// <summary>
        ///* Tunela os dados no grupo.
        /// </summary>
        Group,
        /// <summary>
        ///* Define automaticamente onde os dados devem ser tunelados.
        /// </summary>
        Auto,
        //======================================================
        // - CUSTOM PACKETS ADD HERE.
        //======================================================
        [Obsolete("Do not use this packet, it is only for testing (:", true)] CustomTest,
    }

    /// <summary>
    ///* Define o protocolo que será usado para transmitir os dados.
    /// </summary>
    [Network]
    public enum Protocol : byte
    {
        /// <summary>
        ///* Transmissão confiável e ordenada.
        /// </summary>
        Tcp,
        /// <summary>
        ///* Transmissão não confiável e não ordenada.<br/>
        ///! Em breve dará suporte a transmissão confiável e ordenada(RUDP).
        /// </summary>
        Udp,
        /// <summary>
        /// 
        /// </summary>
        ReliableUdp
    }

    /// <summary>
    ///* Define como os dados serão armazenados no cache.
    /// </summary>
    [Network]
    public enum CacheMode : byte
    {
        /// <summary>
        ///* Não habilita o cache de dados.
        /// </summary>
        None,
        /// <summary>
        ///* O cache é substituído pelos dados mas recentes.
        /// </summary>
        Overwrite,
        /// <summary>
        ///* É criado um novo cache para armazenar os dados.
        /// </summary>
        New
    }
}

namespace NeutronNetwork.Internal.Packets
{
    #region Byte
    [Network]
    public enum Packet : byte
    {
        Empty,
        TcpKeepAlive,
        Handshake,
        NewPlayer,
        Disconnection,
        iRPC,
        gRPC,
        JoinChannel,
        JoinRoom,
        Leave,
        CreateRoom,
        Chat,
        GetChannels,
        GetChached,
        GetRooms,
        Fail,
        DestroyPlayer,
        Nickname,
        SetPlayerProperties,
        SetRoomProperties,
        Ping,
        CustomPacket,
        OnAutoSync,
    }

    /// <summary>
    ///* Define o tipo de objeto.
    /// </summary>
    [Network]
    public enum RegisterMode : byte
    {
        Player,
        Dynamic,
        Scene
    }
    #endregion

    #region Int
    /// <summary>
    ///* Define a compressão de dados que será usado.
    /// </summary>
    public enum CompressionMode : int
    {
        /// <summary>
        ///* Desativa a compressão.
        /// </summary>
        None,
        /// <summary>
        ///* Habilita a compressão Deflate.
        /// </summary>
        Deflate,
        /// <summary>
        ///* Habilita a compressão Gzip.
        /// </summary>
        Gzip,
        /// <summary>
        ///* Compressão de bytes customizada.
        /// </summary>
        Custom
    }

    /// <summary>
    ///* Define o tipo de cliente que será executado.
    /// </summary>
    public enum ClientMode : int
    {
        /// <summary>
        ///* Cliente principal, utilizado para o jogo em geral.
        /// </summary>
        Player,
        /// <summary>
        ///* Cliente secundário, afins de testes e simulação.
        /// </summary>
        Virtual,
    }

    /// <summary>
    ///* Define o tipo de serialização usado.
    /// </summary>
    public enum SerializationMode : int
    {
        /// <summary>
        ///* Serialização binária. 
        /// </summary>
        Binary,
        /// <summary>
        ///* Serialização em texto.
        /// </summary>
        Json,
        /// <summary>
        ///* Implemente sua propria serialização.
        /// </summary>
        Custom,
    }

    public enum StatsSentOrRec : int
    {
        ClientSent,
        ClientRec,
        ServerSent,
        ServerRec
    }

    /// <summary>
    ///* Define quem tem a autoridade sobre o objeto. 
    /// </summary>
    [Flags]
    public enum AuthorityMode : int
    {
        /// <summary>
        ///* Define autoridade personalizada sobre o objeto.
        /// </summary>
        Custom = 0,
        /// <summary>
        ///* Somente o servidor tem a autoridade sobre o objeto.
        /// </summary>
        Server = 1,
        /// <summary>
        ///* Somente o dono do objeto tem a autoridade sobre o objeto.
        /// </summary>
        Mine = 2,
        /// <summary>
        ///* Somente o dono da sala tem a autoridade sobre o objeto.
        /// </summary>
        Master = 8,
        /// <summary>
        ///* Todos os objetos tem a autoridade sobre o objeto.
        /// </summary>
        All = ~0
    }

    public enum SmoothMode : int
    {
        Lerp,
        MoveTowards,
        SmoothDamp,
    }

    public enum SyncOnOff : int
    {
        Sync,
        NonSync
    }

    /// <summary>
    ///* Define em qual lado o objeto deve existir.
    /// </summary>
    public enum Side : int
    {
        /// <summary>
        ///* Objeto só existe ao lado do servidor.
        /// </summary>
        Server,
        /// <summary>
        ///* Objeto só existe ao lado do cliente.
        /// </summary>
        Client,
        /// <summary>
        ///* Objeto existe no lado do cliente e do servidor.
        /// </summary>
        Both
    }

    [Flags]
    public enum MethodType : int
    {
        Void = 0,
        Bool = 1,
        View = 2,
        Int = 4,
        Object = 8,
        String = 16,
        Async = 32,
        Task = 64
    }

    public enum EncodingType : int
    {
        UTF8,
        UTF7,
        UTF32,
        Unicode,
        BigEndianUnicode,
        ASCII,
        Default,
    }

    public enum HeaderSizeType : int
    {
        Byte,
        Int,
        Short,
    }

    public enum ThreadType : int
    {
        Unity,
        Neutron,
    }

    public enum ReceiveType : int
    {
        Asynchronous,
    }

    public enum SendType : int
    {
        Asynchronous,
        Synchronous
    }

    //* https://docs.microsoft.com/pt-br/dotnet/standard/asynchronous-programming-patterns/
    public enum AsynchronousType : int
    {
        TAP,
        APM
    }
    #endregion
}