namespace rtsp_dynamic_gate_app.models
{
	public class RtspSettings
	{
		public string FfmpegPath { get; set; }
		public string NvrIp { get; set; }
		public int Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public bool UseTcp { get; set; }
		public long Probesize { get; set; }
		public long Analyzeduration { get; set; }
		public string VideoCodec { get; set; }
		public string AudioCodec { get; set; }
		public string Preset { get; set; }
		public string ForceKeyFrames { get; set; }
		public int HlsTime { get; set; }
		public int HlsListSize { get; set; }
		public string SegmentTemplate { get; set; }
		public string OutputTemplate { get; set; }
		public List<int> AutoStartChannels { get; set; }

		// Добавим возможность динамически собирать URL
		public string GenerateRtspUrl(string host)
		{
			return $"rtsp://{host}:{Port}/live";
		}
	}
}
