using System;

namespace ClinicManagement
{
    public static class Session
    {
        public static string CurrentUser { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
    }
}
