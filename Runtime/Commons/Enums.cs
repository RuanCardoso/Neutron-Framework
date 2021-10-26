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
    ///* Define os alvos de recepção do pacote.<br/>
    ///* Você pode criar alvos personalizados(OnCustomTarget).
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
        All,
        Server,
        Room,
        Channel,
    }

    /// <summary>
    ///* Define o canal que será usado para transmitir as mensagens.
    /// </summary>
    [Network]
    public enum ChatMode : byte
    {
        /// <summary>
        ///* Envia uma mensagem global, isto inclui todos os canais e salas.
        /// </summary>
        Global,
        /// <summary>
        ///* Envia uma mensagem para um jogador específico.
        /// </summary>
        Private,
    }

    /// <summary>
    ///* Define o pacote a ser obtido do cache.
    /// </summary>
    [Network]
    public enum CachedPacket : byte
    {
        /// <summary>
        ///* Obtém todos ou um pacote específico do cache gRPC.
        /// </summary>
        gRPC = 255,
        /// <summary>
        ///* Obtém todos ou um pacote específico do cache iRPC.
        /// </summary>
        iRPC = 254,
        /// <summary>
        ///* Obtém todos ou um pacote específico do cache Custom.
        /// </summary>
        Custom = 253,
        /// <summary>
        ///* Obtém todos os pacotes do Cache.
        /// </summary>
        All = 100,
    }

    /// <summary>
    ///* Define onde os dados devem ser tunelados.
    /// </summary>
    [Network]
    public enum MatchmakingTo : byte
    {
        /// <summary>
        ///* Usado com <see cref="TargetTo.Me"></see>
        /// </summary>
        Me,
        /// <summary>
        ///* Tunela os dados no servidor, isto inclui todos os canais e salas.
        /// </summary>
        Server,
        /// <summary>
        ///* Tunela os dados no canal, isto inclui as salas que pertencem ao canal.
        /// </summary>
        Channel,
        /// <summary>
        ///* Tunela os dados na sala.
        /// </summary>
        Room,
        /// <summary>
        ///* Define automaticamente onde os dados devem ser tunelados.
        /// </summary>
        Auto,
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
        /// </summary>
        Udp,
        /// <summary>
        ///* Transmissão confiável e ordenada.<br/>
        ///* Em breve.
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
        ///* É criado um novo cache para armazenar os novos dados.
        /// </summary>
        New,
        /// <summary>
        ///* O cache é substituído pelos dados mas recentes, persistente, isto é, o cache só é removido após a destruição do matchmaking.
        /// </summary>
        PersistentOverwrite,
        /// <summary>
        ///* É criado um novo cache para armazenar os novos dados, persistente, isto é, o cache só é removido após a destruição do matchmaking.
        /// </summary>
        PersistentNew
    }
}

namespace NeutronNetwork.Internal.Packets
{
    #region Byte
    [Network]
    public enum Packet : byte
    {
        Empty,
        Handshake,
        Nickname,
        AuthStatus,
        TcpKeepAlive,
        UdpKeepAlive,
        Disconnection,
        iRPC,
        gRPC,
        AutoSync,
        CustomPacket,
        JoinChannel,
        GetChannels,
        GetRooms,
        GetCache,
        JoinRoom,
        CreateRoom,
        SetPlayerProperties,
        SetRoomProperties,
        Leave,
        Synchronize,
        Chat,
        Destroy,
        Error,
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
    public enum AuthorityMode : int
    {
        /// <summary>
        ///* Autoridade baseada na autoridade de outro objeto.
        /// </summary>
        Handled,
        /// <summary>
        ///* Define autoridade personalizada sobre o objeto.
        /// </summary>
        Custom,
        /// <summary>
        ///* Somente o servidor tem a autoridade sobre o objeto.
        /// </summary>
        Server,
        /// <summary>
        ///* Somente o dono do objeto tem a autoridade sobre o objeto.
        /// </summary>
        Mine,
        /// <summary>
        ///* Somente o dono da sala tem a autoridade sobre o objeto.
        /// </summary>
        Master,
        /// <summary>
        ///* Somente o servidor e o dono do objeto tem a autoridade sobre o objeto.
        /// </summary>
        MineAndServer,
        /// <summary>
        ///* Somente o dono da sala e o dono do objeto tem a autoridade sobre o objeto.
        /// </summary>
        MineAndMaster,
        /// <summary>
        ///* Somente o dono da sala e o servidor tem a autoridade sobre o objeto.
        /// </summary>
        ServerAndMaster,
        /// <summary>
        ///* Todos os jogadores tem a autoridade sobre o objeto.
        /// </summary>
        All,
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

    public enum OwnerMode : int
    {
        Server,
        Master
    }

    public enum PhysicsMode : int
    {
        Disabled = 0x0,
        Physics2D = 0x1,
        Physics3D = 0x2,
    }
    #endregion
}