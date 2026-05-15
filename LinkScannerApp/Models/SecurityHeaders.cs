namespace LinkScannerApp.Models
{
    public sealed class SecurityHeaders
    {
        public bool HasCSP { get; set; }
        public bool HasHSTS { get; set; }
        public bool HasXFO { get; set; }
        public bool HasXCTO { get; set; }
        public bool HasReferrerPolicy { get; set; }
        public bool HasPermissionsPolicy { get; set; }
    }
}