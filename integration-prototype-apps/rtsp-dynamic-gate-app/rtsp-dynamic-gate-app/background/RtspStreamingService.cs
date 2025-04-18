using System.Diagnostics;
using Microsoft.Extensions.Options;
using rtsp_dynamic_gate_app.models;

namespace rtsp_dynamic_gate_app.background
{
	public class RtspStreamingService : BackgroundService
	{
		private readonly ILogger<RtspStreamingService> _logger;
		private readonly RtspSettings _settings;
		private readonly Dictionary<int, Process> _ffmpegProcesses = new();

		public RtspStreamingService(
			ILogger<RtspStreamingService> logger,
			IOptions<RtspSettings> settingsOptions)
		{
			_logger = logger;
			_settings = settingsOptions.Value;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("RtspStreamingService started");

			if (_settings.AutoStartChannels?.Any() == true)
			{
				foreach (var ch in _settings.AutoStartChannels)
				{
					try
					{
						StartStream(ch);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to auto-start channel {Channel}", ch);
					}
				}
			}

			return Task.CompletedTask;
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			return StopAllStreamsAsync(cancellationToken);
		}

		public async Task StopAllStreamsAsync(CancellationToken cancellationToken)
		{
			foreach (var channelId in _ffmpegProcesses.Keys.ToList())
			{
				try
				{
					await StopStreamAsync(channelId, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error while stopping stream for channel {ChannelId}", channelId);
				}
			}

			_ffmpegProcesses.Clear();
			_logger.LogInformation("Stopped all FFmpeg processes");
		}

		public async Task StopStreamAsync(int channelId, CancellationToken cancellationToken)
		{
			if (_ffmpegProcesses.TryGetValue(channelId, out var process))
			{
				try
				{
					_logger.LogInformation("Trying to stop stream for channel {ChannelId}", channelId);

					if (!process.HasExited)
					{
						_logger.LogInformation("FFmpeg process for channel {ChannelId} is still running. Killing it...", channelId);

						var startInfo = new ProcessStartInfo
						{
							FileName = "taskkill",
							Arguments = $"/PID {process.Id} /T /F",
							CreateNoWindow = true,
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardError = true
						};

						using (var taskkillProcess = Process.Start(startInfo))
						{
							if (taskkillProcess != null)
							{
								await taskkillProcess.WaitForExitAsync(cancellationToken);

								var output = await taskkillProcess.StandardOutput.ReadToEndAsync();
								var error = await taskkillProcess.StandardError.ReadToEndAsync();

								if (!string.IsNullOrEmpty(output))
									_logger.LogInformation("Taskkill output: {Output}", output);
								if (!string.IsNullOrEmpty(error))
									_logger.LogError("Taskkill error: {Error}", error);
							}
						}

						if (!process.HasExited)
						{
							process.Kill(true);
							await process.WaitForExitAsync(cancellationToken);
							_logger.LogWarning("FFmpeg process for channel {ChannelId} was forcefully killed", channelId);
						}
					}
					else
					{
						_logger.LogInformation("FFmpeg process for channel {ChannelId} already exited", channelId);
					}

					process.Dispose();
					_logger.LogInformation("Stopped stream for channel {ChannelId}", channelId);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error stopping stream for channel {ChannelId}", channelId);
				}
				finally
				{
					_ffmpegProcesses.Remove(channelId);
				}
			}
			else
			{
				_logger.LogWarning("No running stream found for channel {ChannelId}", channelId);
			}
		}

		public bool StartStream(int channelId)
		{
			if (_ffmpegProcesses.ContainsKey(channelId))
			{
				_logger.LogWarning("Stream for channel {ChannelId} is already running", channelId);
				return false;
			}

			// Формируем URL с использованием данных GatewaySettings
			var host = _settings.NvrIp;
			var rtspUrl = _settings.GenerateRtspUrl(host);

			Console.WriteLine("RTSP URL: " + rtspUrl);

			// Путь для сохранения сегментов
			var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "streams");
			Directory.CreateDirectory(outputDirectory);

			var outputPath = Path.Combine(outputDirectory, _settings.OutputTemplate.Replace("{channel}", channelId.ToString()));
			var segmentPath = Path.Combine(outputDirectory, _settings.SegmentTemplate.Replace("{channel}", channelId.ToString()));

			var args = $"-rtsp_transport {(_settings.UseTcp ? "tcp" : "udp")} " +
					   $"-probesize {_settings.Probesize} " +
					   $"-analyzeduration {_settings.Analyzeduration} " +
					   $"-i \"{rtspUrl}\" " +
					   $"-c:v {_settings.VideoCodec} " +
					   $"-c:a {_settings.AudioCodec} " +
					   $"-preset {_settings.Preset} " +
					   $"-force_key_frames \"{_settings.ForceKeyFrames}\" " +
					   $"-f hls " +
					   $"-hls_time {_settings.HlsTime} " +
					   $"-hls_list_size {_settings.HlsListSize} " +
					   $"-hls_segment_filename \"{segmentPath}\" " +
					   $"\"{outputPath}\"";

			var startInfo = new ProcessStartInfo
			{
				FileName = _settings.FfmpegPath,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			var process = new Process { StartInfo = startInfo };
			process.OutputDataReceived += (s, e) => _logger.LogDebug("[FFmpeg STDOUT] {Data}", e.Data);
			process.ErrorDataReceived += (s, e) => _logger.LogDebug("[FFmpeg STDERR] {Data}", e.Data);

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			_ffmpegProcesses[channelId] = process;

			_logger.LogInformation("Started stream for channel {ChannelId}", channelId);

			return true;
		}

		public bool IsStreaming(int channelId)
		{
			return _ffmpegProcesses.ContainsKey(channelId) && !_ffmpegProcesses[channelId].HasExited;
		}

		public bool StartStream(int channelId, int streamType)
		{
			// Можно расширить логику, если streamType влияет на формирование команды
			_logger.LogInformation("Starting stream with type {StreamType} for channel {ChannelId}", streamType, channelId);
			return StartStream(channelId);
		}
	}
}
