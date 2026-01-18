using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Uniforge.FastTrack.Editor
{
    [InitializeOnLoad]
    public class UniforgeServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static bool _isRunning;
        private const int PORT = 7777;

        // Critical: Main thread data exchange
        private static string _pendingData;
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        static UniforgeServer()
        {
            if (_initialized) return;
            _initialized = true;

            // Hook into Editor Update
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnQuitting;

            // Delay start to avoid conflicts during domain reload
            EditorApplication.delayCall += () =>
            {
                if (!_isRunning)
                {
                    StartServer();
                }
            };
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServer();
        }

        private static void OnQuitting()
        {
            StopServer();
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void StartServer()
        {
            if (_isRunning) return;

            // Check if port is available
            if (!IsPortAvailable(PORT))
            {
                Debug.LogWarning($"<color=yellow>[UniforgeServer]</color> Port {PORT} in use. Waiting for release...");

                // Try again after delay
                EditorApplication.delayCall += () =>
                {
                    if (!_isRunning)
                    {
                        StartServerInternal();
                    }
                };
                return;
            }

            StartServerInternal();
        }

        private static void StartServerInternal()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{PORT}/import/");
                _listener.Start();
                _isRunning = true;

                _serverThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "UniforgeServer"
                };
                _serverThread.Start();

                Debug.Log($"<color=cyan>[UniforgeServer]</color> Listening on port {PORT}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniforgeServer] Failed to start: {e.Message}");
                _isRunning = false;
                _listener = null;
            }
        }

        public static void StopServer()
        {
            _isRunning = false;

            try
            {
                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UniforgeServer] Stop warning: {e.Message}");
            }
            finally
            {
                _listener = null;
            }

            // Give thread time to exit
            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join(500);
                _serverThread = null;
            }
        }

        private static void ListenLoop()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    if (!_listener.IsListening) break;

                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Listener stopped - expected during shutdown
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Listener disposed - expected during shutdown
                    break;
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"[UniforgeServer] Loop Error: {e.Message}");
                    }
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            // Add CORS Headers
            res.AddHeader("Access-Control-Allow-Origin", "*");
            res.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            res.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            try
            {
                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 200;
                    res.Close();
                    return;
                }

                if (req.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();

                        lock (_lock)
                        {
                            _pendingData = json;
                        }
                    }

                    res.StatusCode = 200;
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("OK");
                    res.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    res.StatusCode = 405;
                }
            }
            catch (Exception e)
            {
                res.StatusCode = 500;
                Debug.LogError($"[UniforgeServer] Handle Error: {e.Message}");
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        // Main Thread Update
        private static void Update()
        {
            string dataToProcess = null;

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_pendingData))
                {
                    dataToProcess = _pendingData;
                    _pendingData = null;
                }
            }

            if (dataToProcess != null)
            {
                UniforgeImporter.ImportFromJson(dataToProcess);
            }
        }

        [MenuItem("Uniforge/Restart Server")]
        public static void RestartServerMenu()
        {
            StopServer();
            EditorApplication.delayCall += () =>
            {
                StartServer();
                Debug.Log("[UniforgeServer] Manual Restart Complete");
            };
        }

        [MenuItem("Uniforge/Check Status")]
        public static void CheckStatusMenu()
        {
            string status = _isRunning ? "Running" : "Stopped";
            string listenerStatus = _listener != null && _listener.IsListening ? "Listening" : "Not Listening";
            Debug.Log($"[UniforgeServer] Status: {status}, Listener: {listenerStatus}, Port: {PORT}");
        }
    }
}
