using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;  
using Newtonsoft.Json; 
using System.Threading.Tasks;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

public class VRClient : MonoBehaviour
{
    [Header("Отладка")]
    public bool debugLogs = true;

    private TcpClient client;
    private NetworkStream stream;
    private bool isConnected = false;
    
    private readonly ConcurrentQueue<string> _incomingQueue = new();
    private readonly Dictionary<string, TaskCompletionSource<Dictionary<string, object>>> _pendingRequests = new();
    private bool _isReading = false;
    private int _requestId = 0;
    
    public event Action<Dictionary<string, object>> OnMessageReceived;

    void Update()
    {

        while (_incomingQueue.TryDequeue(out string json))
        {
            ProcessMessage(json);
        }
    }

    public async Task<bool> ConnectToServerAsync(string serverIP, int port)
    {

        if (isConnected && client?.Connected == true)
        {
            Log("⚠️ Уже подключено!");
            return true;
        }

        try
        {
            client = new TcpClient();
        
            var connectTask = client.ConnectAsync(serverIP, port);
            bool connected = await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask;
        
            if (connected && client.Connected)
            {
                isConnected = true;
                stream = client.GetStream();
                StartReading();
                Log("Успешное подключение к ПК!");
                return true;  
            }
            else
            {
                ShowError("Таймаут подключения");
                LogError("Таймаут подключения");
                client?.Close();
                return false;
            }
        }
        catch (Exception e)
        {
            ShowError($"Ошибка: {e.Message}");
            LogError($"Ошибка: {e.Message}");
            client?.Close();
            return false;
        }
    }

    private void StartReading()
    {
        if (_isReading) return;
        _isReading = true;
        
        Task.Run(ReadLoop);
    }
    private void ReadLoop()
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuffer = new();
    
        try
        {
            while (isConnected && client?.Connected == true)
            {
                if (stream?.DataAvailable == true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(chunk);

                    string bufferStr = messageBuffer.ToString();
                
                    while (bufferStr.IndexOf('\n') >= 0)
                    {
                        int newlineIndex = bufferStr.IndexOf('\n');
                        string json = bufferStr.Substring(0, newlineIndex).Trim();
                        bufferStr = bufferStr.Substring(newlineIndex + 1);
                    
                        if (!string.IsNullOrEmpty(json))
                        {
                            if (TryParseResponse(json, out string requestId, out var responseData))
                            {
                                CompleteRequest(requestId, responseData);
                            }
                            else
                            {
                                _incomingQueue.Enqueue(json);
                            }
                        }
                    }
                
                    messageBuffer = new StringBuilder(bufferStr);
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
        catch (Exception e)
        {
            if (isConnected)
            {
                ShowError($"Ошибка чтения: {e.Message}");
                LogError($"Ошибка чтения: {e.Message}");
            }
            isConnected = false;
        }
        finally
        {
            _isReading = false;
        }
    }

    private bool TryParseResponse(string json, out string requestId, out Dictionary<string, object> data)
    {
        requestId = null;
        data = null;
        
        try
        {
            data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            
            if (data?.TryGetValue("request_id", out var idObj) == true && idObj is string reqId)
            {
                requestId = reqId;
                return true;
            }
        }
        catch
        {

        }
        
        return false;
    }


    private void CompleteRequest(string requestId, Dictionary<string, object> data)
    {
        if (_pendingRequests.TryGetValue(requestId, out var tcs))
        {
            _pendingRequests.Remove(requestId);
            tcs.TrySetResult(data);
        }
    }

    private void ProcessMessage(string json)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            
            Log($"Сообщение: {json}");
            
            OnMessageReceived?.Invoke(data);
            
            if (data?.TryGetValue("type", out var typeObj) == true && typeObj is string type)
            {
                switch (type)
                {
                    case "ping":
                        SendData(new Dictionary<string, object> { { "type", "pong" } });
                        break;
                    case "server_message":
                        if (data.TryGetValue("text", out var text))
                            Log($"Сервер: {text}");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            ShowError($"Ошибка парсинга: {e.Message}");
            LogError($"Ошибка парсинга: {e.Message}");
        }
    }

    public void SendData(Dictionary<string, object> data)
    {
        if (!isConnected || stream == null || !stream.CanWrite) return;

        try
        {
            string json = JsonConvert.SerializeObject(data, Formatting.None);
            byte[] bytes = Encoding.UTF8.GetBytes(json + "\n"); 
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(); 
        }
        catch (Exception e)
        {
            ShowError($"Ошибка отправки: {e.Message}");
            LogError($"Ошибка отправки: {e.Message}");
            isConnected = false;
        }
    }

    public async Task<Dictionary<string, object>> SendRequestAsync(
        Dictionary<string, object> data, 
        float timeout = 10f)
    {
        if (!isConnected || stream == null)
            throw new Exception("Not connected to server");

        string requestId = $"req_{++_requestId}_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
        data["request_id"] = requestId;

        var tcs = new TaskCompletionSource<Dictionary<string, object>>();
        _pendingRequests[requestId] = tcs;

        SendData(data);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay((int)(timeout * 1000)));
        
        if (completed == tcs.Task)
        {
            _pendingRequests.Remove(requestId);
            return await tcs.Task;
        }
        else
        {
            _pendingRequests.Remove(requestId);
            throw new TimeoutException($"Server did not respond within {timeout} seconds");
        }
    }

    public bool IsConnected() => isConnected && client?.Connected == true;

    private void Log(string message)
    {
        if (debugLogs) Debug.Log($"[VRClient] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[VRClient] {message}");
    }

    void OnApplicationQuit() => Cleanup();
    void OnDisable() => Cleanup();
    
    private void Cleanup()
    {
        isConnected = false;
        _isReading = false;
        
        foreach (var tcs in _pendingRequests.Values)
        {
            tcs.TrySetException(new Exception("Connection closed"));
        }
        _pendingRequests.Clear();
        
        try
        {
            stream?.Close();
            client?.Close();
        }
        catch
        {  

        }
        
        Log("Соединение закрыто");
    }

    public List<object> ConvertJArrayToList(JArray jArray)
    {
        if (jArray == null) return null;

        var result = new List<object>();
        foreach (var item in jArray)
        {
            result.Add(item);
        }
        return result;
    }

    public Dictionary<string, string> ConvertJObjectToDict(JObject jObj)
    {
        if (jObj == null) return null;

        return jObj.Properties().ToDictionary(p => p.Name, p => p.Value?.ToString() ?? "");
    }

    private void ShowError(string message)
    {
        ErrorPopupManager.ShowError(message);
        Debug.LogWarning($"Ошибка валидации: {message}");
    }

    public void Disconnect()
    {
        if (!isConnected && client?.Connected != true)
        {
            Log("⚠️ Уже отключено");
            return;
        }

        Log("🔌 Отключение от сервера...");
        Cleanup(); // Вызываем существующий приватный метод очистки
    }
}