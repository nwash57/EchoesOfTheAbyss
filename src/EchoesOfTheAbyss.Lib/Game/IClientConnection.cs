namespace EchoesOfTheAbyss.Lib.Game;

public interface IClientConnection
{
    Task SendAsync(string json);
}
