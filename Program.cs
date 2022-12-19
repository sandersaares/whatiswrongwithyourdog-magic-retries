await using var server = new TestServer();
server.Start();

var client = new HttpClient();
await client.GetAsync(server.CreateUrl("/disconnect"));