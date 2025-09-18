using System;

namespace ast_visual_studio_extension.CxExtension.Enums
{
    public enum EngineType
    {
        SAST,
        SCA,
        KICS,
        SECRET_DETECTION,
        SCS_SECRET_DETECTION,
        IAC_SECURITY
    }

    public static class EngineTypeExtensions
    {
        public static string ToEngineString(this EngineType type)
        {
            switch (type)
            {
                case EngineType.SAST:
                    return "sast";
                case EngineType.SCA:
                    return "sca";
                case EngineType.KICS:
                    return "kics";
                case EngineType.SECRET_DETECTION:
                    return "secret detection";
                case EngineType.SCS_SECRET_DETECTION:
                    return "sscs-secret-detection";
                case EngineType.IAC_SECURITY:
                    return "IaC Security";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public static EngineType FromEngineString(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "sast":
                    return EngineType.SAST;
                case "sca":
                    return EngineType.SCA;
                case "kics":
                    return EngineType.KICS;
                case "secret detection":
                    return EngineType.SECRET_DETECTION;
                case "sscs-secret-detection":
                    return EngineType.SCS_SECRET_DETECTION;
                case "iac security":
                    return EngineType.IAC_SECURITY;
                default:
                    throw new ArgumentException($"Unknown engine type: {value}", nameof(value));
            }
        }

    }
}
