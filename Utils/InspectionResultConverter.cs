using GVisionWpf.Attributes;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.UIs.ViewModels;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace GVisionWpf.Utils
{
    public static class InspectionResultConverter
    {
        public static EResultType ErrorTypeInEResultType(InspectionResult inspectionResult)
        {
            PropertyInfo[] properties = inspectionResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Result<>))
                {
                    continue;
                }

                var ignoreAttribute = prop.GetCustomAttribute<IgnoreInToStringAttribute>();

                if (ignoreAttribute != null) continue;

                dynamic? value = prop.GetValue(inspectionResult);
                if (value?.Type != EResultType.Good)
                {
                    return value?.Type;
                }
            }

            return EResultType.Good;
        }

        public static EResultType ErrorTypeInEResultType(IInspectionResultModel inspectionResult)
        {
            PropertyInfo[] properties = inspectionResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Result<>))
                {
                    continue;
                }

                var ignoreAttribute = prop.GetCustomAttribute<IgnoreInToStringAttribute>();

                if (ignoreAttribute != null) continue;

                try
                {
                    dynamic? value = prop.GetValue(inspectionResult);
                    if (value?.Type != EResultType.Good)
                    {
                        return value?.Type;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return EResultType.Good;
        }

        public static uint ErrorTypeInPacketCode(InspectionResult inspectionResult, EInspection mode)
        {
            PropertyInfo[] properties = inspectionResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Result<>))
                {
                    continue;
                }

                dynamic? value = prop.GetValue(inspectionResult);
                if (value?.Type != EResultType.Good)
                {
                    return ErrorType2ErrorCode(value?.Type, mode);
                }
            }

            return ErrorType2ErrorCode(EResultType.Good, mode);
        }

        public static uint ErrorType2ErrorCode(EResultType resultType, EInspection mode)
        {
            bool isDefinedErrorCode = GlobalSetting.Instance.VisionResult[mode].TryGetValue(resultType, out uint errorCode);

            if (!isDefinedErrorCode)
            {
                GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[{mode}] 정의되지 않은 에러타입 : {resultType}");
                return 1;
            }

            switch (GlobalSetting.Instance.SystemType)
            {
                case ESystemType.HanaMicron:
                    switch (mode)
                    {
                        case EInspection.Mark:
                            if (resultType == EResultType.NoDevice) return 42; // Package Size
                            else if (resultType == EResultType.CornerDegree) return 55; // Chipping
                            else if (resultType == EResultType.Contamination) return 54; // Foreign Material 
                            

                            else if (resultType == EResultType.TextAngle) return 47; // Wrong Char
                            else if (resultType == EResultType.TextOffset) return 47; // Wrong Char
                            else if (resultType == EResultType.MissingChar) return 47; // Wrong Char
                            else if (resultType == EResultType.DataCode) return 47; // BTL에서 사용 X

                            // else if (resultType == EResultType.SawOffset) return -1; // Setting에서 사용 X


                            break;
                        case EInspection.Bga:
                            if (resultType == EResultType.Contamination) return 20;// Scratch
                            else if (resultType == EResultType.ForeignMaterial) return 20; // Scratch
                            else if (resultType == EResultType.BallCount) return 14; // Missing Ball
                            else if (resultType == EResultType.BallBridging) return 16; // Missing Ball

                            else if (resultType == EResultType.Chipping) return 13; // SawOffset
                            else if (resultType == EResultType.Burr) return 13; // SawOffset

                            else if (resultType == EResultType.CornerDegree) return 13; // SawOffset

                            else if (resultType == EResultType.FirstPin) return 21; // Incomp Ball, BTL과 협의 필요

                            // else if (resultType == EResultType.Pattern) return -1; // Setting에서 사용 X
                                break;

                        default:
                            break;
                    }
                    
                    break;
                default:
                    break;
            }

            return errorCode;
        }

        public static bool EvaluateResults(InspectionResult inspectionResult)
        {
            if (GlobalSetting.Instance.Inspection.Mode == EInspectionMode.AllPass)
            {
                return true;
            }

            bool result = true;
            PropertyInfo[] properties = inspectionResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Result<>))
                {
                    continue;
                }

                dynamic? value = prop.GetValue(inspectionResult);
                result &= value?.Type == EResultType.Good;
            }

            return result;
        }

        public static bool EvaluateResults(IInspectionResultModel inspectionResult)
        {
            if (GlobalSetting.Instance.Inspection.Mode == EInspectionMode.AllPass)
            {
                return true;
            }

            bool result = true;
            PropertyInfo[] properties = inspectionResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Result<>))
                {
                    continue;
                }

                dynamic? value = prop.GetValue(inspectionResult);
                result &= value?.Type == EResultType.Good;
            }

            return result;
        }

        public static string ToString(InspectionResult inspectionResult)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(addSpacesToSentence(inspectionResult.GetType().Name).ToUpper());
            appendProperties(sb, inspectionResult.GetType().BaseType, inspectionResult);
            appendProperties(sb, inspectionResult.GetType(), inspectionResult);

            return sb.ToString();
        }

        private static void appendProperties(StringBuilder sb, Type? type, InspectionResult inspectionResult)
        {
            if (type == null) return;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                var ignoreAttribute = prop.GetCustomAttribute<IgnoreInToStringAttribute>();

                if (ignoreAttribute != null) continue;

                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    dynamic? value = prop.GetValue(inspectionResult);
                    sb.AppendLine($"{addSpacesToSentence(prop.Name)} : {value?.ToString()}");
                }
                else
                {
                    sb.AppendLine($"{addSpacesToSentence(prop.Name)} : {prop.GetValue(inspectionResult)}");
                }
            }
        }

        private static string addSpacesToSentence(string text)
        {
            return Regex.Replace(text, "(\\B[A-Z])", " $1");
        }
    }
}