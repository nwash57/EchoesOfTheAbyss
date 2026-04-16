namespace EchoesOfTheAbyss.Lib.Llm;

public class LlmConfig
{
	public string Host { get; set; }
	public int Port { get; set; }
	public string Model { get; set; }

	public LlmConfig(string host, int port, string model)
	{
		Host = host;
		Port = port;
		Model = model;
	}
}