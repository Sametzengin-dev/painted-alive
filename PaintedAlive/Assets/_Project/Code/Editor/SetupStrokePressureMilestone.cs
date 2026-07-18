using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    [InitializeOnLoad]
    public static class SetupStrokePressureMilestone
    {
        private const string PendingKey =
            "PaintedAlive.SetupStrokePressureMilestone.Pending";

        private const string PaintOilFolder =
            "Assets/_Project/Code/Runtime/Paint/Oil";

        private const string PaintersFolder =
            "Assets/_Project/Code/Runtime/Painters";

        private const string UiFolder =
            "Assets/_Project/Code/Runtime/UI";

        private const string DataFolder =
            "Assets/_Project/Data/Painters";

        private const string PressureProfilePath =
            PaintOilFolder + "/OilStrokePressureProfile.cs";

        private const string PressureConfigPath =
            PaintersFolder + "/PainterStrokePressureConfig.cs";

        private const string PressureTrackerPath =
            PaintersFolder + "/PainterStrokePressureTracker.cs";

        private const string PressureHudPath =
            UiFolder + "/PainterStrokePressureHud.cs";

        private const string OilStrokeRuntimePath =
            PaintOilFolder + "/OilStrokeRuntime.cs";

        private const string OilStrokeSystemPath =
            PaintOilFolder + "/OilStrokeSystem.cs";

        private const string StrokeBudgetPath =
            PaintersFolder + "/PainterStrokeBudget.cs";

        private const string BrushControllerPath =
            PaintersFolder + "/PainterBrushController.cs";

        private const string PressureAssetPath =
            DataFolder + "/DA_OilPainterStrokePressure_Default.asset";

        private const string PressureProfileBase64 =
            "dXNpbmcgU3lzdGVtOwp1c2luZyBVbml0eUVuZ2luZTsKCm5hbWVzcGFjZSBQYWludGVkQWxpdmUuUGFpbnQKewogICAgW1NlcmlhbGl6YWJsZV0KICAgIHB1YmxpYyBzdHJ1Y3QgT2lsU3Ryb2tlUHJlc3N1cmVQcm9maWxlCiAgICB7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIGZsb2F0IGF2ZXJhZ2VEcmF3U3BlZWQ7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIGZsb2F0IHByZXNzdXJlTm9ybWFsaXplZDsKICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgZmxvYXQgd2lkdGhNdWx0aXBsaWVyOwogICAgICAgIFtTZXJpYWxpemVGaWVsZF0gcHJpdmF0ZSBmbG9hdCBoZWlnaHRNdWx0aXBsaWVyOwogICAgICAgIFtTZXJpYWxpemVGaWVsZF0gcHJpdmF0ZSBmbG9hdCBwaWdtZW50TXVsdGlwbGllcjsKICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgZmxvYXQgY3V0UmVzaXN0YW5jZU11bHRpcGxpZXI7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIGZsb2F0IGxpZmVjeWNsZUR1cmF0aW9uTXVsdGlwbGllcjsKICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgZmxvYXQgYnVkZ2V0TXVsdGlwbGllcjsKCiAgICAgICAgcHVibGljIGZsb2F0IEF2ZXJhZ2VEcmF3U3BlZWQgPT4gYXZlcmFnZURyYXdTcGVlZDsKICAgICAgICBwdWJsaWMgZmxvYXQgUHJlc3N1cmVOb3JtYWxpemVkID0+IHByZXNzdXJlTm9ybWFsaXplZDsKICAgICAgICBwdWJsaWMgZmxvYXQgV2lkdGhNdWx0aXBsaWVyID0+IHdpZHRoTXVsdGlwbGllcjsKICAgICAgICBwdWJsaWMgZmxvYXQgSGVpZ2h0TXVsdGlwbGllciA9PiBoZWlnaHRNdWx0aXBsaWVyOwogICAgICAgIHB1YmxpYyBmbG9hdCBQaWdtZW50TXVsdGlwbGllciA9PiBwaWdtZW50TXVsdGlwbGllcjsKCiAgICAgICAgcHVibGljIGZsb2F0IEN1dFJlc2lzdGFuY2VNdWx0aXBsaWVyID0+CiAgICAgICAgICAgIGN1dFJlc2lzdGFuY2VNdWx0aXBsaWVyOwoKICAgICAgICBwdWJsaWMgZmxvYXQgTGlmZWN5Y2xlRHVyYXRpb25NdWx0aXBsaWVyID0+CiAgICAgICAgICAgIGxpZmVjeWNsZUR1cmF0aW9uTXVsdGlwbGllcjsKCiAgICAgICAgcHVibGljIGZsb2F0IEJ1ZGdldE11bHRpcGxpZXIgPT4gYnVkZ2V0TXVsdGlwbGllcjsKCiAgICAgICAgcHVibGljIGJvb2wgSXNWYWxpZCA9PgogICAgICAgICAgICB3aWR0aE11bHRpcGxpZXIgPiAwZiAmJgogICAgICAgICAgICBoZWlnaHRNdWx0aXBsaWVyID4gMGYgJiYKICAgICAgICAgICAgcGlnbWVudE11bHRpcGxpZXIgPiAwZiAmJgogICAgICAgICAgICBjdXRSZXNpc3RhbmNlTXVsdGlwbGllciA+IDBmICYmCiAgICAgICAgICAgIGxpZmVjeWNsZUR1cmF0aW9uTXVsdGlwbGllciA+IDBmICYmCiAgICAgICAgICAgIGJ1ZGdldE11bHRpcGxpZXIgPiAwZjsKCiAgICAgICAgcHVibGljIE9pbFN0cm9rZVByZXNzdXJlUHJvZmlsZSgKICAgICAgICAgICAgZmxvYXQgZHJhd1NwZWVkLAogICAgICAgICAgICBmbG9hdCBwcmVzc3VyZSwKICAgICAgICAgICAgZmxvYXQgd2lkdGgsCiAgICAgICAgICAgIGZsb2F0IGhlaWdodCwKICAgICAgICAgICAgZmxvYXQgcGlnbWVudCwKICAgICAgICAgICAgZmxvYXQgY3V0UmVzaXN0YW5jZSwKICAgICAgICAgICAgZmxvYXQgbGlmZWN5Y2xlRHVyYXRpb24sCiAgICAgICAgICAgIGZsb2F0IGJ1ZGdldCkKICAgICAgICB7CiAgICAgICAgICAgIGF2ZXJhZ2VEcmF3U3BlZWQgPSBNYXRoZi5NYXgoMGYsIGRyYXdTcGVlZCk7CiAgICAgICAgICAgIHByZXNzdXJlTm9ybWFsaXplZCA9IE1hdGhmLkNsYW1wMDEocHJlc3N1cmUpOwogICAgICAgICAgICB3aWR0aE11bHRpcGxpZXIgPSBNYXRoZi5NYXgoMC4xZiwgd2lkdGgpOwogICAgICAgICAgICBoZWlnaHRNdWx0aXBsaWVyID0gTWF0aGYuTWF4KDAuMWYsIGhlaWdodCk7CiAgICAgICAgICAgIHBpZ21lbnRNdWx0aXBsaWVyID0gTWF0aGYuTWF4KDAuMWYsIHBpZ21lbnQpOwogICAgICAgICAgICBjdXRSZXNpc3RhbmNlTXVsdGlwbGllciA9IE1hdGhmLk1heCgwLjFmLCBjdXRSZXNpc3RhbmNlKTsKICAgICAgICAgICAgbGlmZWN5Y2xlRHVyYXRpb25NdWx0aXBsaWVyID0gTWF0aGYuTWF4KDAuMWYsIGxpZmVjeWNsZUR1cmF0aW9uKTsKICAgICAgICAgICAgYnVkZ2V0TXVsdGlwbGllciA9IE1hdGhmLk1heCgwLjFmLCBidWRnZXQpOwogICAgICAgIH0KCiAgICAgICAgcHVibGljIHN0YXRpYyBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUgQmFsYW5jZWQgPT4KICAgICAgICAgICAgbmV3KAogICAgICAgICAgICAgICAgMi41ZiwKICAgICAgICAgICAgICAgIDAuNWYsCiAgICAgICAgICAgICAgICAxZiwKICAgICAgICAgICAgICAgIDFmLAogICAgICAgICAgICAgICAgMWYsCiAgICAgICAgICAgICAgICAxZiwKICAgICAgICAgICAgICAgIDFmLAogICAgICAgICAgICAgICAgMWYpOwogICAgfQp9Cg==";

        private const string PressureConfigBase64 =
            "dXNpbmcgUGFpbnRlZEFsaXZlLlBhaW50Owp1c2luZyBVbml0eUVuZ2luZTsKCm5hbWVzcGFjZSBQYWludGVkQWxpdmUuUGFpbnRlcnMKewogICAgW0NyZWF0ZUFzc2V0TWVudSgKICAgICAgICBmaWxlTmFtZSA9ICJQYWludGVyU3Ryb2tlUHJlc3N1cmVDb25maWciLAogICAgICAgIG1lbnVOYW1lID0gIlBhaW50ZWQgQWxpdmUvUGFpbnRlcnMvU3Ryb2tlIFByZXNzdXJlIENvbmZpZyIpXQogICAgcHVibGljIHNlYWxlZCBjbGFzcyBQYWludGVyU3Ryb2tlUHJlc3N1cmVDb25maWcgOiBTY3JpcHRhYmxlT2JqZWN0CiAgICB7CiAgICAgICAgW0hlYWRlcigiRHJhdyBTcGVlZCIpXQogICAgICAgIFtTZXJpYWxpemVGaWVsZCwgTWluKDAuMDVmKV0KICAgICAgICBwcml2YXRlIGZsb2F0IHNsb3dEcmF3U3BlZWQgPSAwLjc1ZjsKCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkLCBNaW4oMC4xZildCiAgICAgICAgcHJpdmF0ZSBmbG9hdCBmYXN0RHJhd1NwZWVkID0gNmY7CgogICAgICAgIFtTZXJpYWxpemVGaWVsZCwgTWluKDAuMDVmKV0KICAgICAgICBwcml2YXRlIGZsb2F0IGRlZmF1bHREcmF3U3BlZWQgPSAyLjVmOwoKICAgICAgICBbSGVhZGVyKCJTYW1wbGluZyIpXQogICAgICAgIFtTZXJpYWxpemVGaWVsZCwgTWluKDAuMDJmKV0KICAgICAgICBwcml2YXRlIGZsb2F0IG1heGltdW1TZWdtZW50U2FtcGxlRHVyYXRpb24gPSAwLjc1ZjsKCiAgICAgICAgW0hlYWRlcigiUHJlc3N1cmUgQ3VydmVzIildCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXQogICAgICAgIHByaXZhdGUgQW5pbWF0aW9uQ3VydmUgd2lkdGhCeVByZXNzdXJlID0KICAgICAgICAgICAgQW5pbWF0aW9uQ3VydmUuTGluZWFyKDBmLCAwLjY1ZiwgMWYsIDEuMzVmKTsKCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXQogICAgICAgIHByaXZhdGUgQW5pbWF0aW9uQ3VydmUgaGVpZ2h0QnlQcmVzc3VyZSA9CiAgICAgICAgICAgIEFuaW1hdGlvbkN1cnZlLkxpbmVhcigwZiwgMC43MGYsIDFmLCAxLjI1Zik7CgogICAgICAgIFtTZXJpYWxpemVGaWVsZF0KICAgICAgICBwcml2YXRlIEFuaW1hdGlvbkN1cnZlIHBpZ21lbnRCeVByZXNzdXJlID0KICAgICAgICAgICAgQW5pbWF0aW9uQ3VydmUuTGluZWFyKDBmLCAwLjcwZiwgMWYsIDEuNDBmKTsKCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXQogICAgICAgIHByaXZhdGUgQW5pbWF0aW9uQ3VydmUgY3V0UmVzaXN0YW5jZUJ5UHJlc3N1cmUgPQogICAgICAgICAgICBBbmltYXRpb25DdXJ2ZS5MaW5lYXIoMGYsIDAuNjBmLCAxZiwgMS42MGYpOwoKICAgICAgICBbU2VyaWFsaXplRmllbGRdCiAgICAgICAgcHJpdmF0ZSBBbmltYXRpb25DdXJ2ZSBsaWZlY3ljbGVCeVByZXNzdXJlID0KICAgICAgICAgICAgQW5pbWF0aW9uQ3VydmUuTGluZWFyKDBmLCAwLjc1ZiwgMWYsIDEuMjVmKTsKCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXQogICAgICAgIHByaXZhdGUgQW5pbWF0aW9uQ3VydmUgYnVkZ2V0QnlQcmVzc3VyZSA9CiAgICAgICAgICAgIEFuaW1hdGlvbkN1cnZlLkxpbmVhcigwZiwgMC44MGYsIDFmLCAxLjI1Zik7CgogICAgICAgIHB1YmxpYyBmbG9hdCBEZWZhdWx0RHJhd1NwZWVkID0+IGRlZmF1bHREcmF3U3BlZWQ7CgogICAgICAgIHB1YmxpYyBmbG9hdCBNYXhpbXVtU2VnbWVudFNhbXBsZUR1cmF0aW9uID0+CiAgICAgICAgICAgIG1heGltdW1TZWdtZW50U2FtcGxlRHVyYXRpb247CgogICAgICAgIHB1YmxpYyBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUgRXZhbHVhdGUoCiAgICAgICAgICAgIGZsb2F0IGF2ZXJhZ2VEcmF3U3BlZWQpCiAgICAgICAgewogICAgICAgICAgICBhdmVyYWdlRHJhd1NwZWVkID0KICAgICAgICAgICAgICAgIE1hdGhmLk1heCgwZiwgYXZlcmFnZURyYXdTcGVlZCk7CgogICAgICAgICAgICBmbG9hdCBzcGVlZE5vcm1hbGl6ZWQgPSBNYXRoZi5JbnZlcnNlTGVycCgKICAgICAgICAgICAgICAgIHNsb3dEcmF3U3BlZWQsCiAgICAgICAgICAgICAgICBmYXN0RHJhd1NwZWVkLAogICAgICAgICAgICAgICAgYXZlcmFnZURyYXdTcGVlZCk7CgogICAgICAgICAgICAvLyBZYXZhxZ8gw6dpemdpIHnDvGtzZWsgYmFzxLFuw6d0xLFyLgogICAgICAgICAgICBmbG9hdCBwcmVzc3VyZSA9CiAgICAgICAgICAgICAgICAxZiAtIHNwZWVkTm9ybWFsaXplZDsKCiAgICAgICAgICAgIHJldHVybiBuZXcgT2lsU3Ryb2tlUHJlc3N1cmVQcm9maWxlKAogICAgICAgICAgICAgICAgYXZlcmFnZURyYXdTcGVlZCwKICAgICAgICAgICAgICAgIHByZXNzdXJlLAogICAgICAgICAgICAgICAgRXZhbHVhdGVDdXJ2ZSh3aWR0aEJ5UHJlc3N1cmUsIHByZXNzdXJlLCAxZiksCiAgICAgICAgICAgICAgICBFdmFsdWF0ZUN1cnZlKGhlaWdodEJ5UHJlc3N1cmUsIHByZXNzdXJlLCAxZiksCiAgICAgICAgICAgICAgICBFdmFsdWF0ZUN1cnZlKHBpZ21lbnRCeVByZXNzdXJlLCBwcmVzc3VyZSwgMWYpLAogICAgICAgICAgICAgICAgRXZhbHVhdGVDdXJ2ZSgKICAgICAgICAgICAgICAgICAgICBjdXRSZXNpc3RhbmNlQnlQcmVzc3VyZSwKICAgICAgICAgICAgICAgICAgICBwcmVzc3VyZSwKICAgICAgICAgICAgICAgICAgICAxZiksCiAgICAgICAgICAgICAgICBFdmFsdWF0ZUN1cnZlKAogICAgICAgICAgICAgICAgICAgIGxpZmVjeWNsZUJ5UHJlc3N1cmUsCiAgICAgICAgICAgICAgICAgICAgcHJlc3N1cmUsCiAgICAgICAgICAgICAgICAgICAgMWYpLAogICAgICAgICAgICAgICAgRXZhbHVhdGVDdXJ2ZSgKICAgICAgICAgICAgICAgICAgICBidWRnZXRCeVByZXNzdXJlLAogICAgICAgICAgICAgICAgICAgIHByZXNzdXJlLAogICAgICAgICAgICAgICAgICAgIDFmKSk7CiAgICAgICAgfQoKICAgICAgICBwcml2YXRlIHN0YXRpYyBmbG9hdCBFdmFsdWF0ZUN1cnZlKAogICAgICAgICAgICBBbmltYXRpb25DdXJ2ZSBjdXJ2ZSwKICAgICAgICAgICAgZmxvYXQgdmFsdWUsCiAgICAgICAgICAgIGZsb2F0IGZhbGxiYWNrKQogICAgICAgIHsKICAgICAgICAgICAgcmV0dXJuIGN1cnZlICE9IG51bGwgJiYgY3VydmUubGVuZ3RoID4gMAogICAgICAgICAgICAgICAgPyBNYXRoZi5NYXgoMC4xZiwgY3VydmUuRXZhbHVhdGUodmFsdWUpKQogICAgICAgICAgICAgICAgOiBmYWxsYmFjazsKICAgICAgICB9CgogICAgICAgIHByaXZhdGUgdm9pZCBPblZhbGlkYXRlKCkKICAgICAgICB7CiAgICAgICAgICAgIHNsb3dEcmF3U3BlZWQgPQogICAgICAgICAgICAgICAgTWF0aGYuTWF4KDAuMDVmLCBzbG93RHJhd1NwZWVkKTsKCiAgICAgICAgICAgIGZhc3REcmF3U3BlZWQgPQogICAgICAgICAgICAgICAgTWF0aGYuTWF4KAogICAgICAgICAgICAgICAgICAgIHNsb3dEcmF3U3BlZWQgKyAwLjA1ZiwKICAgICAgICAgICAgICAgICAgICBmYXN0RHJhd1NwZWVkKTsKCiAgICAgICAgICAgIGRlZmF1bHREcmF3U3BlZWQgPSBNYXRoZi5DbGFtcCgKICAgICAgICAgICAgICAgIGRlZmF1bHREcmF3U3BlZWQsCiAgICAgICAgICAgICAgICBzbG93RHJhd1NwZWVkLAogICAgICAgICAgICAgICAgZmFzdERyYXdTcGVlZCk7CgogICAgICAgICAgICBtYXhpbXVtU2VnbWVudFNhbXBsZUR1cmF0aW9uID0KICAgICAgICAgICAgICAgIE1hdGhmLk1heCgKICAgICAgICAgICAgICAgICAgICAwLjAyZiwKICAgICAgICAgICAgICAgICAgICBtYXhpbXVtU2VnbWVudFNhbXBsZUR1cmF0aW9uKTsKICAgICAgICB9CiAgICB9Cn0K";

        private const string PressureTrackerBase64 =
            "dXNpbmcgUGFpbnRlZEFsaXZlLlBhaW50Owp1c2luZyBVbml0eUVuZ2luZTsKCm5hbWVzcGFjZSBQYWludGVkQWxpdmUuUGFpbnRlcnMKewogICAgW0Rpc2FsbG93TXVsdGlwbGVDb21wb25lbnRdCiAgICBwdWJsaWMgc2VhbGVkIGNsYXNzIFBhaW50ZXJTdHJva2VQcmVzc3VyZVRyYWNrZXIgOiBNb25vQmVoYXZpb3VyCiAgICB7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXQogICAgICAgIHByaXZhdGUgUGFpbnRlclN0cm9rZVByZXNzdXJlQ29uZmlnIGNvbmZpZzsKCiAgICAgICAgW0hlYWRlcigiUnVudGltZSAtIFJlYWQgT25seSIpXQogICAgICAgIFtTZXJpYWxpemVGaWVsZF0gcHJpdmF0ZSBmbG9hdCBhY2N1bXVsYXRlZERpc3RhbmNlOwogICAgICAgIFtTZXJpYWxpemVGaWVsZF0gcHJpdmF0ZSBmbG9hdCBhY2N1bXVsYXRlZE1vdGlvblRpbWU7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIGZsb2F0IGF2ZXJhZ2VEcmF3U3BlZWQ7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIE9pbFN0cm9rZVByZXNzdXJlUHJvZmlsZSBjdXJyZW50UHJvZmlsZTsKCiAgICAgICAgcHJpdmF0ZSBWZWN0b3IzIHByZXZpb3VzUG9pbnQ7CiAgICAgICAgcHJpdmF0ZSBmbG9hdCBwcmV2aW91c1BvaW50VGltZTsKICAgICAgICBwcml2YXRlIGJvb2wgaXNUcmFja2luZzsKCiAgICAgICAgcHVibGljIGJvb2wgSXNUcmFja2luZyA9PiBpc1RyYWNraW5nOwogICAgICAgIHB1YmxpYyBmbG9hdCBBdmVyYWdlRHJhd1NwZWVkID0+IGF2ZXJhZ2VEcmF3U3BlZWQ7CgogICAgICAgIHB1YmxpYyBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUgQ3VycmVudFByb2ZpbGUgPT4KICAgICAgICAgICAgY3VycmVudFByb2ZpbGUuSXNWYWxpZAogICAgICAgICAgICAgICAgPyBjdXJyZW50UHJvZmlsZQogICAgICAgICAgICAgICAgOiBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUuQmFsYW5jZWQ7CgogICAgICAgIHB1YmxpYyBmbG9hdCBQcmVzc3VyZU5vcm1hbGl6ZWQgPT4KICAgICAgICAgICAgQ3VycmVudFByb2ZpbGUuUHJlc3N1cmVOb3JtYWxpemVkOwoKICAgICAgICBwcml2YXRlIHZvaWQgQXdha2UoKQogICAgICAgIHsKICAgICAgICAgICAgUmVzZXRUcmFja2luZygpOwogICAgICAgIH0KCiAgICAgICAgcHVibGljIHZvaWQgQmVnaW5UcmFja2luZyhWZWN0b3IzIHN0YXJ0UG9pbnQpCiAgICAgICAgewogICAgICAgICAgICBpc1RyYWNraW5nID0gdHJ1ZTsKCiAgICAgICAgICAgIGFjY3VtdWxhdGVkRGlzdGFuY2UgPSAwZjsKICAgICAgICAgICAgYWNjdW11bGF0ZWRNb3Rpb25UaW1lID0gMGY7CgogICAgICAgICAgICBwcmV2aW91c1BvaW50ID0gc3RhcnRQb2ludDsKICAgICAgICAgICAgcHJldmlvdXNQb2ludFRpbWUgPSBUaW1lLnVuc2NhbGVkVGltZTsKCiAgICAgICAgICAgIGF2ZXJhZ2VEcmF3U3BlZWQgPQogICAgICAgICAgICAgICAgY29uZmlnICE9IG51bGwKICAgICAgICAgICAgICAgICAgICA/IGNvbmZpZy5EZWZhdWx0RHJhd1NwZWVkCiAgICAgICAgICAgICAgICAgICAgOiBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUKICAgICAgICAgICAgICAgICAgICAgICAgLkJhbGFuY2VkCiAgICAgICAgICAgICAgICAgICAgICAgIC5BdmVyYWdlRHJhd1NwZWVkOwoKICAgICAgICAgICAgY3VycmVudFByb2ZpbGUgPQogICAgICAgICAgICAgICAgY29uZmlnICE9IG51bGwKICAgICAgICAgICAgICAgICAgICA/IGNvbmZpZy5FdmFsdWF0ZShhdmVyYWdlRHJhd1NwZWVkKQogICAgICAgICAgICAgICAgICAgIDogT2lsU3Ryb2tlUHJlc3N1cmVQcm9maWxlLkJhbGFuY2VkOwogICAgICAgIH0KCiAgICAgICAgcHVibGljIHZvaWQgUmVjb3JkUG9pbnQoVmVjdG9yMyBwb2ludCkKICAgICAgICB7CiAgICAgICAgICAgIGlmICghaXNUcmFja2luZykKICAgICAgICAgICAgewogICAgICAgICAgICAgICAgQmVnaW5UcmFja2luZyhwb2ludCk7CiAgICAgICAgICAgICAgICByZXR1cm47CiAgICAgICAgICAgIH0KCiAgICAgICAgICAgIGZsb2F0IGRpc3RhbmNlID0KICAgICAgICAgICAgICAgIFZlY3RvcjMuRGlzdGFuY2UoCiAgICAgICAgICAgICAgICAgICAgcHJldmlvdXNQb2ludCwKICAgICAgICAgICAgICAgICAgICBwb2ludCk7CgogICAgICAgICAgICBmbG9hdCBjdXJyZW50VGltZSA9CiAgICAgICAgICAgICAgICBUaW1lLnVuc2NhbGVkVGltZTsKCiAgICAgICAgICAgIGZsb2F0IGVsYXBzZWQgPQogICAgICAgICAgICAgICAgY3VycmVudFRpbWUgLSBwcmV2aW91c1BvaW50VGltZTsKCiAgICAgICAgICAgIGlmIChkaXN0YW5jZSA+IDAuMDAwMWYpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIGZsb2F0IG1heGltdW1EdXJhdGlvbiA9CiAgICAgICAgICAgICAgICAgICAgY29uZmlnICE9IG51bGwKICAgICAgICAgICAgICAgICAgICAgICAgPyBjb25maWcuTWF4aW11bVNlZ21lbnRTYW1wbGVEdXJhdGlvbgogICAgICAgICAgICAgICAgICAgICAgICA6IDAuNzVmOwoKICAgICAgICAgICAgICAgIGVsYXBzZWQgPSBNYXRoZi5DbGFtcCgKICAgICAgICAgICAgICAgICAgICBlbGFwc2VkLAogICAgICAgICAgICAgICAgICAgIDAuMDFmLAogICAgICAgICAgICAgICAgICAgIG1heGltdW1EdXJhdGlvbik7CgogICAgICAgICAgICAgICAgYWNjdW11bGF0ZWREaXN0YW5jZSArPSBkaXN0YW5jZTsKICAgICAgICAgICAgICAgIGFjY3VtdWxhdGVkTW90aW9uVGltZSArPSBlbGFwc2VkOwoKICAgICAgICAgICAgICAgIGF2ZXJhZ2VEcmF3U3BlZWQgPQogICAgICAgICAgICAgICAgICAgIGFjY3VtdWxhdGVkTW90aW9uVGltZSA+IDBmCiAgICAgICAgICAgICAgICAgICAgICAgID8gYWNjdW11bGF0ZWREaXN0YW5jZSAvCiAgICAgICAgICAgICAgICAgICAgICAgICAgYWNjdW11bGF0ZWRNb3Rpb25UaW1lCiAgICAgICAgICAgICAgICAgICAgICAgIDogMGY7CgogICAgICAgICAgICAgICAgY3VycmVudFByb2ZpbGUgPQogICAgICAgICAgICAgICAgICAgIGNvbmZpZyAhPSBudWxsCiAgICAgICAgICAgICAgICAgICAgICAgID8gY29uZmlnLkV2YWx1YXRlKAogICAgICAgICAgICAgICAgICAgICAgICAgICAgYXZlcmFnZURyYXdTcGVlZCkKICAgICAgICAgICAgICAgICAgICAgICAgOiBPaWxTdHJva2VQcmVzc3VyZVByb2ZpbGUuQmFsYW5jZWQ7CiAgICAgICAgICAgIH0KCiAgICAgICAgICAgIHByZXZpb3VzUG9pbnQgPSBwb2ludDsKICAgICAgICAgICAgcHJldmlvdXNQb2ludFRpbWUgPSBjdXJyZW50VGltZTsKICAgICAgICB9CgogICAgICAgIHB1YmxpYyB2b2lkIFJlc2V0VHJhY2tpbmcoKQogICAgICAgIHsKICAgICAgICAgICAgaXNUcmFja2luZyA9IGZhbHNlOwoKICAgICAgICAgICAgYWNjdW11bGF0ZWREaXN0YW5jZSA9IDBmOwogICAgICAgICAgICBhY2N1bXVsYXRlZE1vdGlvblRpbWUgPSAwZjsKICAgICAgICAgICAgYXZlcmFnZURyYXdTcGVlZCA9IDBmOwoKICAgICAgICAgICAgY3VycmVudFByb2ZpbGUgPQogICAgICAgICAgICAgICAgT2lsU3Ryb2tlUHJlc3N1cmVQcm9maWxlLkJhbGFuY2VkOwogICAgICAgIH0KICAgIH0KfQo=";

        private const string PressureHudBase64 =
            "dXNpbmcgUGFpbnRlZEFsaXZlLlBhaW50ZXJzOwp1c2luZyBVbml0eUVuZ2luZTsKdXNpbmcgVW5pdHlFbmdpbmUuVUk7CgpuYW1lc3BhY2UgUGFpbnRlZEFsaXZlLlVJCnsKICAgIHB1YmxpYyBzZWFsZWQgY2xhc3MgUGFpbnRlclN0cm9rZVByZXNzdXJlSHVkIDogTW9ub0JlaGF2aW91cgogICAgewogICAgICAgIFtTZXJpYWxpemVGaWVsZF0KICAgICAgICBwcml2YXRlIFBhaW50ZXJCcnVzaENvbnRyb2xsZXIgYnJ1c2hDb250cm9sbGVyOwoKICAgICAgICBbU2VyaWFsaXplRmllbGRdCiAgICAgICAgcHJpdmF0ZSBQYWludGVyU3Ryb2tlUHJlc3N1cmVUcmFja2VyIHByZXNzdXJlVHJhY2tlcjsKCiAgICAgICAgW0hlYWRlcigiVUkiKV0KICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgQ2FudmFzR3JvdXAgY2FudmFzR3JvdXA7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIFNsaWRlciBwcmVzc3VyZVNsaWRlcjsKICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgVGV4dCBzdHlsZVRleHQ7CiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIFRleHQgc3BlZWRUZXh0OwoKICAgICAgICBbSGVhZGVyKCJDb2xvcnMiKV0KICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgQ29sb3IgZmFzdENvbG9yID0KICAgICAgICAgICAgbmV3KDAuOTBmLCAwLjU4ZiwgMC4yMGYsIDFmKTsKCiAgICAgICAgW1NlcmlhbGl6ZUZpZWxkXSBwcml2YXRlIENvbG9yIGJhbGFuY2VkQ29sb3IgPQogICAgICAgICAgICBuZXcoMC45MGYsIDAuODJmLCAwLjYzZiwgMWYpOwoKICAgICAgICBbU2VyaWFsaXplRmllbGRdIHByaXZhdGUgQ29sb3IgaGVhdnlDb2xvciA9CiAgICAgICAgICAgIG5ldygwLjY1ZiwgMC4xMGYsIDAuMTZmLCAxZik7CgogICAgICAgIHByaXZhdGUgdm9pZCBBd2FrZSgpCiAgICAgICAgewogICAgICAgICAgICBpZiAocHJlc3N1cmVTbGlkZXIgIT0gbnVsbCkKICAgICAgICAgICAgewogICAgICAgICAgICAgICAgcHJlc3N1cmVTbGlkZXIubWluVmFsdWUgPSAwZjsKICAgICAgICAgICAgICAgIHByZXNzdXJlU2xpZGVyLm1heFZhbHVlID0gMWY7CiAgICAgICAgICAgICAgICBwcmVzc3VyZVNsaWRlci5pbnRlcmFjdGFibGUgPSBmYWxzZTsKICAgICAgICAgICAgfQogICAgICAgIH0KCiAgICAgICAgcHJpdmF0ZSB2b2lkIE9uRW5hYmxlKCkKICAgICAgICB7CiAgICAgICAgICAgIFNldFZpc2libGUodHJ1ZSk7CiAgICAgICAgICAgIFJlZnJlc2goKTsKICAgICAgICB9CgogICAgICAgIHByaXZhdGUgdm9pZCBPbkRpc2FibGUoKQogICAgICAgIHsKICAgICAgICAgICAgU2V0VmlzaWJsZShmYWxzZSk7CiAgICAgICAgfQoKICAgICAgICBwcml2YXRlIHZvaWQgVXBkYXRlKCkKICAgICAgICB7CiAgICAgICAgICAgIFJlZnJlc2goKTsKICAgICAgICB9CgogICAgICAgIHByaXZhdGUgdm9pZCBSZWZyZXNoKCkKICAgICAgICB7CiAgICAgICAgICAgIGlmIChicnVzaENvbnRyb2xsZXIgPT0gbnVsbCB8fAogICAgICAgICAgICAgICAgcHJlc3N1cmVUcmFja2VyID09IG51bGwpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIHJldHVybjsKICAgICAgICAgICAgfQoKICAgICAgICAgICAgaWYgKCFicnVzaENvbnRyb2xsZXIuSXNQcmV2aWV3aW5nKQogICAgICAgICAgICB7CiAgICAgICAgICAgICAgICBpZiAocHJlc3N1cmVTbGlkZXIgIT0gbnVsbCkKICAgICAgICAgICAgICAgICAgICBwcmVzc3VyZVNsaWRlci52YWx1ZSA9IDAuNWY7CgogICAgICAgICAgICAgICAgaWYgKHN0eWxlVGV4dCAhPSBudWxsKQogICAgICAgICAgICAgICAgewogICAgICAgICAgICAgICAgICAgIHN0eWxlVGV4dC50ZXh0ID0KICAgICAgICAgICAgICAgICAgICAgICAgIsOHxLBaxLBNIEJBU0lOQ0kg4oCUIjsKICAgICAgICAgICAgICAgICAgICBzdHlsZVRleHQuY29sb3IgPSBiYWxhbmNlZENvbG9yOwogICAgICAgICAgICAgICAgfQoKICAgICAgICAgICAgICAgIGlmIChzcGVlZFRleHQgIT0gbnVsbCkKICAgICAgICAgICAgICAgICAgICBzcGVlZFRleHQudGV4dCA9IHN0cmluZy5FbXB0eTsKCiAgICAgICAgICAgICAgICByZXR1cm47CiAgICAgICAgICAgIH0KCiAgICAgICAgICAgIGZsb2F0IHByZXNzdXJlID0KICAgICAgICAgICAgICAgIHByZXNzdXJlVHJhY2tlci5QcmVzc3VyZU5vcm1hbGl6ZWQ7CgogICAgICAgICAgICBpZiAocHJlc3N1cmVTbGlkZXIgIT0gbnVsbCkKICAgICAgICAgICAgICAgIHByZXNzdXJlU2xpZGVyLnZhbHVlID0gcHJlc3N1cmU7CgogICAgICAgICAgICBzdHJpbmcgc3R5bGU7CiAgICAgICAgICAgIENvbG9yIGNvbG9yOwoKICAgICAgICAgICAgaWYgKHByZXNzdXJlIDwgMC4zM2YpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIHN0eWxlID0gIkhJWkxJIOKAoiDEsE5DRSAvIEtJUklMR0FOIjsKICAgICAgICAgICAgICAgIGNvbG9yID0gZmFzdENvbG9yOwogICAgICAgICAgICB9CiAgICAgICAgICAgIGVsc2UgaWYgKHByZXNzdXJlIDwgMC42NmYpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIHN0eWxlID0gIkRFTkdFTMSwIOKAoiBTVEFOREFSVCI7CiAgICAgICAgICAgICAgICBjb2xvciA9IGJhbGFuY2VkQ29sb3I7CiAgICAgICAgICAgIH0KICAgICAgICAgICAgZWxzZQogICAgICAgICAgICB7CiAgICAgICAgICAgICAgICBzdHlsZSA9ICJBxJ5JUiDigKIgS0FMSU4gLyBEQVlBTklLTEkiOwogICAgICAgICAgICAgICAgY29sb3IgPSBoZWF2eUNvbG9yOwogICAgICAgICAgICB9CgogICAgICAgICAgICBpZiAoc3R5bGVUZXh0ICE9IG51bGwpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIHN0eWxlVGV4dC50ZXh0ID0gc3R5bGU7CiAgICAgICAgICAgICAgICBzdHlsZVRleHQuY29sb3IgPSBjb2xvcjsKICAgICAgICAgICAgfQoKICAgICAgICAgICAgaWYgKHNwZWVkVGV4dCAhPSBudWxsKQogICAgICAgICAgICB7CiAgICAgICAgICAgICAgICBmbG9hdCBzcGVlZCA9CiAgICAgICAgICAgICAgICAgICAgcHJlc3N1cmVUcmFja2VyLkF2ZXJhZ2VEcmF3U3BlZWQ7CgogICAgICAgICAgICAgICAgZmxvYXQgcGlnbWVudE11bHRpcGxpZXIgPQogICAgICAgICAgICAgICAgICAgIHByZXNzdXJlVHJhY2tlcgogICAgICAgICAgICAgICAgICAgICAgICAuQ3VycmVudFByb2ZpbGUKICAgICAgICAgICAgICAgICAgICAgICAgLlBpZ21lbnRNdWx0aXBsaWVyOwoKICAgICAgICAgICAgICAgIHNwZWVkVGV4dC50ZXh0ID0KICAgICAgICAgICAgICAgICAgICAkIkhJWiB7c3BlZWQ6MC4wfSBtL3MgIOKAoiAgIiArCiAgICAgICAgICAgICAgICAgICAgJCJQxLBHTUVOVCDDl3twaWdtZW50TXVsdGlwbGllcjowLjAwfSI7CiAgICAgICAgICAgIH0KICAgICAgICB9CgogICAgICAgIHByaXZhdGUgdm9pZCBTZXRWaXNpYmxlKGJvb2wgdmlzaWJsZSkKICAgICAgICB7CiAgICAgICAgICAgIGlmIChjYW52YXNHcm91cCA9PSBudWxsKQogICAgICAgICAgICAgICAgcmV0dXJuOwoKICAgICAgICAgICAgY2FudmFzR3JvdXAuYWxwaGEgPSB2aXNpYmxlID8gMWYgOiAwZjsKICAgICAgICAgICAgY2FudmFzR3JvdXAuaW50ZXJhY3RhYmxlID0gZmFsc2U7CiAgICAgICAgICAgIGNhbnZhc0dyb3VwLmJsb2Nrc1JheWNhc3RzID0gZmFsc2U7CiAgICAgICAgfQogICAgfQp9Cg==";

        static SetupStrokePressureMilestone()
        {
            if (EditorPrefs.GetBool(PendingKey, false))
            {
                EditorApplication.delayCall += TryRunSceneSetup;
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Painters/Setup Stroke Pressure Milestone")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "Kurulum Play Mode dışında çalıştırılmalıdır.");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning(
                    "Unity şu anda derleme yapıyor. Derleme bitince tekrar çalıştır.");
                return;
            }

            try
            {
                RunCodeSetup();
            }
            catch (Exception exception)
            {
                EditorPrefs.DeleteKey(PendingKey);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Stroke Pressure kurulamadı",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Painters/Retry Stroke Pressure Scene Setup")]
        public static void RetrySceneSetup()
        {
            EditorPrefs.SetBool(PendingKey, true);
            TryRunSceneSetup();
        }

        private static void RunCodeSetup()
        {
            ValidateRequiredFile(OilStrokeRuntimePath);
            ValidateRequiredFile(OilStrokeSystemPath);
            ValidateRequiredFile(StrokeBudgetPath);
            ValidateRequiredFile(BrushControllerPath);

            EnsureFolder(PaintOilFolder);
            EnsureFolder(PaintersFolder);
            EnsureFolder(UiFolder);
            EnsureFolder(DataFolder);

            string backupDirectory = CreateBackupDirectory();

            BackupFile(OilStrokeRuntimePath, backupDirectory);
            BackupFile(OilStrokeSystemPath, backupDirectory);
            BackupFile(StrokeBudgetPath, backupDirectory);
            BackupFile(BrushControllerPath, backupDirectory);
            BackupFile(PressureProfilePath, backupDirectory);
            BackupFile(PressureConfigPath, backupDirectory);
            BackupFile(PressureTrackerPath, backupDirectory);
            BackupFile(PressureHudPath, backupDirectory);

            Dictionary<string, string> stagedFiles =
                new Dictionary<string, string>
                {
                    [PressureProfilePath] =
                        DecodeSource(PressureProfileBase64),
                    [PressureConfigPath] =
                        DecodeSource(PressureConfigBase64),
                    [PressureTrackerPath] =
                        DecodeSource(PressureTrackerBase64),
                    [PressureHudPath] =
                        DecodeSource(PressureHudBase64),
                    [OilStrokeRuntimePath] =
                        PatchOilStrokeRuntime(
                            ReadNormalized(OilStrokeRuntimePath)),
                    [OilStrokeSystemPath] =
                        PatchOilStrokeSystem(
                            ReadNormalized(OilStrokeSystemPath)),
                    [StrokeBudgetPath] =
                        PatchPainterStrokeBudget(
                            ReadNormalized(StrokeBudgetPath)),
                    [BrushControllerPath] =
                        PatchPainterBrushController(
                            ReadNormalized(BrushControllerPath))
                };

            foreach (KeyValuePair<string, string> file in stagedFiles)
            {
                WriteNormalized(file.Key, file.Value);
            }

            EditorPrefs.SetBool(PendingKey, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "Stroke pressure kod aşaması tamamlandı. " +
                "Unity derlemesi bittikten sonra asset, component, HUD ve " +
                "test işaretleri otomatik kurulacak.\n" +
                "Yedek klasörü: " + backupDirectory);
        }

        private static string PatchOilStrokeRuntime(string code)
        {
            if (!code.Contains(
                    "private OilStrokePressureProfile pressureProfile"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"(?m)^(\s*)private float lifecycleElapsed;\s*$",
                    match =>
                        match.Value + "\n\n" +
                        match.Groups[1].Value +
                        "private OilStrokePressureProfile pressureProfile =\n" +
                        match.Groups[1].Value +
                        "    OilStrokePressureProfile.Balanced;",
                    "OilStrokeRuntime pressureProfile alanı");
            }

            if (!code.Contains(
                    "public OilStrokePressureProfile PressureProfile"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"public OilStrokeShape Shape\s*\{\s*get;\s*private set;\s*\}",
                    match =>
                        match.Value + "\n\n" +
                        "        public OilStrokePressureProfile PressureProfile =>\n" +
                        "            pressureProfile;",
                    "OilStrokeRuntime PressureProfile property");
            }

            if (!code.Contains(
                    "OilStrokePressureProfile strokePressureProfile"))
            {
                const string initializePattern =
                    @"public\s+void\s+Initialize\s*\(\s*" +
                    @"OilStrokeConfig\s+strokeConfig\s*,\s*" +
                    @"Material\s+initialWetMaterial\s*,\s*" +
                    @"Material\s+finalDryMaterial\s*,\s*" +
                    @"OilStrokeShape\s+strokeShape\s*\)";

                const string replacement =
                    "public void Initialize(\n" +
                    "            OilStrokeConfig strokeConfig,\n" +
                    "            Material initialWetMaterial,\n" +
                    "            Material finalDryMaterial,\n" +
                    "            OilStrokeShape strokeShape)\n" +
                    "        {\n" +
                    "            Initialize(\n" +
                    "                strokeConfig,\n" +
                    "                initialWetMaterial,\n" +
                    "                finalDryMaterial,\n" +
                    "                strokeShape,\n" +
                    "                OilStrokePressureProfile.Balanced);\n" +
                    "        }\n\n" +
                    "        public void Initialize(\n" +
                    "            OilStrokeConfig strokeConfig,\n" +
                    "            Material initialWetMaterial,\n" +
                    "            Material finalDryMaterial,\n" +
                    "            OilStrokeShape strokeShape,\n" +
                    "            OilStrokePressureProfile strokePressureProfile)";

                code = ReplaceRegexRequired(
                    code,
                    initializePattern,
                    _ => replacement,
                    "OilStrokeRuntime Initialize overload");
            }

            if (!code.Contains(
                    "strokePressureProfile.IsValid"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"(?m)^(\s*)Shape\s*=\s*strokeShape;\s*$",
                    match =>
                        match.Value + "\n\n" +
                        match.Groups[1].Value +
                        "pressureProfile =\n" +
                        match.Groups[1].Value +
                        "    strokePressureProfile.IsValid\n" +
                        match.Groups[1].Value +
                        "        ? strokePressureProfile\n" +
                        match.Groups[1].Value +
                        "        : OilStrokePressureProfile.Balanced;",
                    "OilStrokeRuntime pressureProfile assignment");
            }

            if (!code.Contains("float lifecycleMultiplier ="))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"float\s+wetEnd\s*=\s*config\.WetDuration\s*;\s*" +
                    @"float\s+dryEnd\s*=\s*wetEnd\s*\+\s*" +
                    @"config\.DryingDuration\s*;",
                    _ =>
                        "float lifecycleMultiplier =\n" +
                        "                Mathf.Max(\n" +
                        "                    0.1f,\n" +
                        "                    pressureProfile.LifecycleDurationMultiplier);\n\n" +
                        "            float wetEnd =\n" +
                        "                config.WetDuration *\n" +
                        "                lifecycleMultiplier;\n\n" +
                        "            float dryingDuration =\n" +
                        "                config.DryingDuration *\n" +
                        "                lifecycleMultiplier;\n\n" +
                        "            float dryEnd =\n" +
                        "                wetEnd + dryingDuration;",
                    "OilStrokeRuntime lifecycle calculation");
            }

            code = ReplaceOptional(
                code,
                @"if\s*\(\s*config\.DryingDuration\s*>\s*0f\s*&&",
                "if (dryingDuration > 0f &&");

            if (!Regex.IsMatch(
                    code,
                    @"config\.GetWidth\(Shape\)\s*\*\s*" +
                    @"pressureProfile\.WidthMultiplier"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"float\s+halfWidth\s*=\s*" +
                    @"config\.GetWidth\(Shape\)\s*\*\s*0\.5f\s*;",
                    _ =>
                        "float halfWidth =\n" +
                        "                config.GetWidth(Shape) *\n" +
                        "                pressureProfile.WidthMultiplier *\n" +
                        "                0.5f;",
                    "OilStrokeRuntime width multiplier");
            }

            if (!Regex.IsMatch(
                    code,
                    @"config\.GetHeight\([\s\S]*?\)\s*\*\s*" +
                    @"pressureProfile\.HeightMultiplier"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"float\s+sampleHeight\s*=\s*" +
                    @"config\.GetHeight\(\s*Shape\s*,\s*t\s*\)\s*;",
                    _ =>
                        "float sampleHeight =\n" +
                        "                    config.GetHeight(\n" +
                        "                        Shape,\n" +
                        "                        t) *\n" +
                        "                    pressureProfile.HeightMultiplier;",
                    "OilStrokeRuntime height multiplier");
            }

            if (!code.Contains(
                    "pressureProfile.CutResistanceMultiplier"))
            {
                code = ReplaceMethodRequired(
                    code,
                    @"private\s+float\s+GetCutMultiplier\s*\(\s*\)",
                    "private float GetCutMultiplier()\n" +
                    "        {\n" +
                    "            float stateMultiplier = State switch\n" +
                    "            {\n" +
                    "                OilStrokeState.Wet =>\n" +
                    "                    config.WetCutMultiplier,\n\n" +
                    "                OilStrokeState.Drying =>\n" +
                    "                    config.DryingCutMultiplier,\n\n" +
                    "                OilStrokeState.Dry =>\n" +
                    "                    config.DryCutMultiplier,\n\n" +
                    "                _ => 1f\n" +
                    "            };\n\n" +
                    "            float resistance =\n" +
                    "                Mathf.Max(\n" +
                    "                    0.1f,\n" +
                    "                    pressureProfile.CutResistanceMultiplier);\n\n" +
                    "            return stateMultiplier / resistance;\n" +
                    "        }",
                    "OilStrokeRuntime GetCutMultiplier");
            }

            return code;
        }

        private static string PatchOilStrokeSystem(string code)
        {
            if (!code.Contains(
                    "OilStrokePressureProfile pressureProfile)"))
            {
                string previewOverload =
                    "\n\n        public float GetPreviewWidth(\n" +
                    "            OilStrokeShape shape,\n" +
                    "            OilStrokePressureProfile pressureProfile)\n" +
                    "        {\n" +
                    "            float baseWidth =\n" +
                    "                GetPreviewWidth(shape);\n\n" +
                    "            float multiplier =\n" +
                    "                pressureProfile.IsValid\n" +
                    "                    ? pressureProfile.WidthMultiplier\n" +
                    "                    : 1f;\n\n" +
                    "            return baseWidth * multiplier;\n" +
                    "        }";

                code = InsertAfterMemberRequired(
                    code,
                    @"public\s+float\s+GetPreviewWidth\s*\(\s*" +
                    @"OilStrokeShape\s+shape\s*\)",
                    previewOverload,
                    "OilStrokeSystem GetPreviewWidth overload");

                const string beginPattern =
                    @"public\s+bool\s+BeginStroke\s*\(\s*" +
                    @"Vector3\s+worldPoint\s*,\s*" +
                    @"OilStrokeShape\s+shape\s*\)";

                const string beginReplacement =
                    "public bool BeginStroke(\n" +
                    "            Vector3 worldPoint,\n" +
                    "            OilStrokeShape shape)\n" +
                    "        {\n" +
                    "            return BeginStroke(\n" +
                    "                worldPoint,\n" +
                    "                shape,\n" +
                    "                OilStrokePressureProfile.Balanced);\n" +
                    "        }\n\n" +
                    "        public bool BeginStroke(\n" +
                    "            Vector3 worldPoint,\n" +
                    "            OilStrokeShape shape,\n" +
                    "            OilStrokePressureProfile pressureProfile)";

                code = ReplaceRegexRequired(
                    code,
                    beginPattern,
                    _ => beginReplacement,
                    "OilStrokeSystem BeginStroke overload");
            }

            if (!Regex.IsMatch(
                    code,
                    @"activeStroke\.Initialize\(\s*config\s*,\s*" +
                    @"wetMaterial\s*,\s*dryMaterial\s*,\s*shape\s*,\s*" +
                    @"pressureProfile\s*\)"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"activeStroke\.Initialize\(\s*config\s*,\s*" +
                    @"wetMaterial\s*,\s*dryMaterial\s*,\s*shape\s*\)\s*;",
                    _ =>
                        "activeStroke.Initialize(\n" +
                        "                config,\n" +
                        "                wetMaterial,\n" +
                        "                dryMaterial,\n" +
                        "                shape,\n" +
                        "                pressureProfile);",
                    "OilStrokeSystem runtime Initialize call");
            }

            return code;
        }

        private static string PatchPainterStrokeBudget(string code)
        {
            if (!code.Contains(
                    "stroke.PressureProfile.BudgetMultiplier"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"pressure\s*\+=\s*isDry\s*\?\s*" +
                    @"config\.DryStrokePressure\s*:\s*" +
                    @"config\.GetActivePressure\(stroke\.Shape\)\s*;",
                    _ =>
                        "if (isDry)\n" +
                        "                    {\n" +
                        "                        pressure += config.DryStrokePressure;\n" +
                        "                    }\n" +
                        "                    else\n" +
                        "                    {\n" +
                        "                        float profileMultiplier =\n" +
                        "                            stroke.PressureProfile.IsValid\n" +
                        "                                ? stroke.PressureProfile.BudgetMultiplier\n" +
                        "                                : 1f;\n\n" +
                        "                        pressure +=\n" +
                        "                            config.GetActivePressure(stroke.Shape) *\n" +
                        "                            profileMultiplier;\n" +
                        "                    }",
                    "PainterStrokeBudget CurrentPressure profile multiplier");
            }

            if (!code.Contains(
                    "OilStrokePressureProfile pressureProfile,"))
            {
                string replacement =
                    "public bool CanBeginStroke(\n" +
                    "            OilStrokeShape shape,\n" +
                    "            out PainterStrokeBlockReason reason)\n" +
                    "        {\n" +
                    "            return CanBeginStroke(\n" +
                    "                shape,\n" +
                    "                OilStrokePressureProfile.Balanced,\n" +
                    "                out reason);\n" +
                    "        }\n\n" +
                    "        public bool CanBeginStroke(\n" +
                    "            OilStrokeShape shape,\n" +
                    "            OilStrokePressureProfile pressureProfile,\n" +
                    "            out PainterStrokeBlockReason reason)\n" +
                    "        {\n" +
                    "            reason = PainterStrokeBlockReason.None;\n\n" +
                    "            if (config == null || strokeSystem == null)\n" +
                    "                return false;\n\n" +
                    "            if (strokeSystem.IsDrawing)\n" +
                    "            {\n" +
                    "                reason =\n" +
                    "                    PainterStrokeBlockReason.StrokeInProgress;\n\n" +
                    "                return false;\n" +
                    "            }\n\n" +
                    "            if (CooldownRemaining > 0f)\n" +
                    "            {\n" +
                    "                reason =\n" +
                    "                    PainterStrokeBlockReason.Cooldown;\n\n" +
                    "                return false;\n" +
                    "            }\n\n" +
                    "            if (ActiveStrokeCount >=\n" +
                    "                config.MaximumActiveStrokes)\n" +
                    "            {\n" +
                    "                reason =\n" +
                    "                    PainterStrokeBlockReason.ActiveStrokeLimit;\n\n" +
                    "                return false;\n" +
                    "            }\n\n" +
                    "            float profileMultiplier =\n" +
                    "                pressureProfile.IsValid\n" +
                    "                    ? pressureProfile.BudgetMultiplier\n" +
                    "                    : 1f;\n\n" +
                    "            float projectedPressure =\n" +
                    "                CurrentPressure +\n" +
                    "                config.GetActivePressure(shape) *\n" +
                    "                profileMultiplier;\n\n" +
                    "            if (projectedPressure >\n" +
                    "                config.MaximumPressure + 0.001f)\n" +
                    "            {\n" +
                    "                reason =\n" +
                    "                    PainterStrokeBlockReason.PressureLimit;\n\n" +
                    "                return false;\n" +
                    "            }\n\n" +
                    "            return true;\n" +
                    "        }";

                code = ReplaceMethodRequired(
                    code,
                    @"public\s+bool\s+CanBeginStroke\s*\(\s*" +
                    @"OilStrokeShape\s+shape\s*,\s*" +
                    @"out\s+PainterStrokeBlockReason\s+reason\s*\)",
                    replacement,
                    "PainterStrokeBudget CanBeginStroke overload");
            }

            return code;
        }

        private static string PatchPainterBrushController(string code)
        {
            if (!code.Contains(
                    "private PainterStrokePressureTracker pressureTracker;"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"(?m)^(\s*)\[SerializeField\]\s*" +
                    @"private PainterStrokeBudget strokeBudget;\s*$",
                    match =>
                        match.Value + "\n\n" +
                        match.Groups[1].Value +
                        "[SerializeField]\n" +
                        match.Groups[1].Value +
                        "private PainterStrokePressureTracker pressureTracker;",
                    "PainterBrushController pressureTracker field");
            }

            if (!code.Contains(
                    "public OilStrokePressureProfile CurrentPressureProfile"))
            {
                code = ReplaceRegexRequired(
                    code,
                    @"(?m)^(\s*)private void Awake\(\)\s*$",
                    match =>
                        match.Groups[1].Value +
                        "public OilStrokePressureProfile CurrentPressureProfile =>\n" +
                        match.Groups[1].Value +
                        "    pressureTracker != null\n" +
                        match.Groups[1].Value +
                        "        ? pressureTracker.CurrentProfile\n" +
                        match.Groups[1].Value +
                        "        : OilStrokePressureProfile.Balanced;\n\n" +
                        match.Value,
                    "PainterBrushController CurrentPressureProfile property");
            }

            code = TransformMethodRequired(
                code,
                @"private\s+void\s+StartPreview\s*\(",
                method =>
                {
                    if (!method.Contains("pressureTracker.BeginTracking"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"previewPoints\.Clear\(\)\s*;\s*" +
                            @"previewPoints\.Add\(startPoint\)\s*;",
                            _ =>
                                "previewPoints.Clear();\n" +
                                "            previewPoints.Add(startPoint);\n\n" +
                                "            if (pressureTracker != null)\n" +
                                "            {\n" +
                                "                pressureTracker.BeginTracking(startPoint);\n" +
                                "            }",
                            "StartPreview pressure tracking");
                    }

                    if (!method.Contains("RefreshPreviewWidth();"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"float\s+previewWidth\s*=\s*" +
                            @"strokeSystem\.GetPreviewWidth\(shape\)\s*;\s*" +
                            @"(?<worldSpace>" +
                            @"strokePreview\.useWorldSpace\s*=\s*true\s*;\s*)?" +
                            @"strokePreview\.startWidth\s*=\s*" +
                            @"Mathf\.Clamp\([\s\S]*?\)\s*;\s*" +
                            @"strokePreview\.endWidth\s*=\s*" +
                            @"strokePreview\.startWidth\s*;",
                            match =>
                            {
                                string worldSpaceAssignment =
                                    match.Groups["worldSpace"].Success
                                        ? "strokePreview.useWorldSpace = true;\n\n" +
                                          "            "
                                        : string.Empty;

                                return worldSpaceAssignment +
                                       "RefreshPreviewWidth();";
                            },
                            "StartPreview preview width");
                    }

                    return method;
                },
                "PainterBrushController StartPreview");

            code = TransformMethodRequired(
                code,
                @"private\s+void\s+AppendPreviewPoint\s*\(",
                method =>
                {
                    if (!method.Contains("pressureTracker.RecordPoint"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"previewPoints\.Add\(point\)\s*;",
                            _ =>
                                "previewPoints.Add(point);\n\n" +
                                "            if (pressureTracker != null)\n" +
                                "            {\n" +
                                "                pressureTracker.RecordPoint(point);\n" +
                                "            }\n\n" +
                                "            RefreshPreviewWidth();",
                            "AppendPreviewPoint pressure tracking");
                    }

                    return method;
                },
                "PainterBrushController AppendPreviewPoint");

            if (!code.Contains("private void RefreshPreviewWidth()"))
            {
                string method =
                    "\n\n        private void RefreshPreviewWidth()\n" +
                    "        {\n" +
                    "            if (strokePreview == null ||\n" +
                    "                strokeSystem == null)\n" +
                    "            {\n" +
                    "                return;\n" +
                    "            }\n\n" +
                    "            float previewWidth =\n" +
                    "                strokeSystem.GetPreviewWidth(\n" +
                    "                    activeShape,\n" +
                    "                    CurrentPressureProfile);\n\n" +
                    "            strokePreview.startWidth =\n" +
                    "                Mathf.Clamp(\n" +
                    "                    previewWidth * 0.65f,\n" +
                    "                    0.12f,\n" +
                    "                    1.5f);\n\n" +
                    "            strokePreview.endWidth =\n" +
                    "                strokePreview.startWidth;\n" +
                    "        }";

                code = InsertAfterMethodRequired(
                    code,
                    @"private\s+void\s+AppendPreviewPoint\s*\(",
                    method,
                    "PainterBrushController RefreshPreviewWidth method");
            }

            code = TransformMethodRequired(
                code,
                @"private\s+void\s+CommitPreview\s*\(",
                method =>
                {
                    if (!Regex.IsMatch(
                            method,
                            @"BeginStroke\([\s\S]*?CurrentPressureProfile"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"strokeSystem\.BeginStroke\(\s*" +
                            @"previewPoints\[0\]\s*,\s*activeShape\s*\)",
                            _ =>
                                "strokeSystem.BeginStroke(\n" +
                                "                    previewPoints[0],\n" +
                                "                    activeShape,\n" +
                                "                    CurrentPressureProfile)",
                            "CommitPreview pressure profile");
                    }

                    return method;
                },
                "PainterBrushController CommitPreview");

            code = TransformMethodRequired(
                code,
                @"private\s+void\s+FinishPreview\s*\(",
                method =>
                {
                    if (!Regex.IsMatch(
                            method,
                            @"CanBeginStroke\([\s\S]*?CurrentPressureProfile"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"strokeBudget\.CanBeginStroke\(\s*" +
                            @"activeShape\s*,\s*out\s+_\s*\)",
                            _ =>
                                "strokeBudget.CanBeginStroke(\n" +
                                "                    activeShape,\n" +
                                "                    CurrentPressureProfile,\n" +
                                "                    out _)",
                            "FinishPreview pressure budget check");
                    }

                    return method;
                },
                "PainterBrushController FinishPreview");

            code = TransformMethodRequired(
                code,
                @"private\s+float\s+CalculatePreviewCost\s*\(",
                method =>
                {
                    if (!method.Contains("pressurePigmentMultiplier"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"return\s+Mathf\.Max\(\s*0f\s*,\s*cost\s*\)\s*;",
                            _ =>
                                "float pressurePigmentMultiplier =\n" +
                                "                CurrentPressureProfile.IsValid\n" +
                                "                    ? CurrentPressureProfile.PigmentMultiplier\n" +
                                "                    : 1f;\n\n" +
                                "            return Mathf.Max(\n" +
                                "                0f,\n" +
                                "                cost * pressurePigmentMultiplier);",
                            "CalculatePreviewCost pressure pigment multiplier");
                    }

                    return method;
                },
                "PainterBrushController CalculatePreviewCost");

            code = TransformMethodRequired(
                code,
                @"private\s+void\s+ClearPreview\s*\(",
                method =>
                {
                    if (!method.Contains("pressureTracker.ResetTracking"))
                    {
                        method = ReplaceRegexRequired(
                            method,
                            @"previewPoints\.Clear\(\)\s*;",
                            _ =>
                                "previewPoints.Clear();\n\n" +
                                "            if (pressureTracker != null)\n" +
                                "            {\n" +
                                "                pressureTracker.ResetTracking();\n" +
                                "            }",
                            "ClearPreview pressure reset");
                    }

                    return method;
                },
                "PainterBrushController ClearPreview");

            return code;
        }

        private static void TryRunSceneSetup()
        {
            if (!EditorPrefs.GetBool(PendingKey, false))
                return;

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRunSceneSetup;
                return;
            }

            Type configType =
                FindTypeDerivedFrom<ScriptableObject>(
                    "PainterStrokePressureConfig");

            Type trackerType =
                FindTypeDerivedFrom<MonoBehaviour>(
                    "PainterStrokePressureTracker");

            Type hudType =
                FindTypeDerivedFrom<MonoBehaviour>(
                    "PainterStrokePressureHud");

            if (configType == null ||
                trackerType == null ||
                hudType == null)
            {
                Debug.LogError(
                    "Stroke pressure runtime sınıfları derlenemedi. " +
                    "Console'daki ilk kırmızı hatayı düzelt. " +
                    "Düzeltmeden sonra menüden 'Retry Stroke Pressure Scene Setup' çalıştır.");
                return;
            }

            try
            {
                ScriptableObject configAsset =
                    CreateOrUpdatePressureConfig(configType);

                RuntimeReferences runtime =
                    ConfigurePaintRuntime(
                        trackerType,
                        configAsset);

                Component hud =
                    ConfigurePressureHud(
                        hudType,
                        runtime);

                AddToPainterBehaviours(hud);
                CreatePressureTestMarkers();

                AssetDatabase.SaveAssets();

                EditorSceneManager.MarkSceneDirty(
                    SceneManager.GetActiveScene());

                EditorPrefs.DeleteKey(PendingKey);

                Debug.Log(
                    "Stroke pressure milestone kurulumu tamamlandı: " +
                    "kodlar, config asset, PaintRuntime bağlantıları, HUD, " +
                    "role switcher listesi ve 4 metrelik test işaretleri hazır.");

                EditorUtility.DisplayDialog(
                    "Stroke Pressure hazır",
                    "Kurulum tamamlandı. Sahneyi Ctrl+S ile kaydet ve Play Mode testine geç.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError(
                    "Sahne kurulum aşaması tamamlanamadı. " +
                    "Kod aşaması korunuyor. Sorunu düzelttikten sonra Retry menüsünü çalıştır.");
            }
        }

        private static ScriptableObject CreateOrUpdatePressureConfig(
            Type configType)
        {
            EnsureFolder(DataFolder);

            ScriptableObject asset =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    PressureAssetPath);

            if (asset == null)
            {
                asset =
                    ScriptableObject.CreateInstance(configType);

                asset.name =
                    "DA_OilPainterStrokePressure_Default";

                AssetDatabase.CreateAsset(
                    asset,
                    PressureAssetPath);
            }
            else if (!configType.IsInstanceOfType(asset))
            {
                throw new InvalidOperationException(
                    "Pressure config yolunda farklı türde bir asset var: " +
                    PressureAssetPath);
            }

            SerializedObject serialized =
                new SerializedObject(asset);

            SetFloat(serialized, "slowDrawSpeed", 0.75f);
            SetFloat(serialized, "fastDrawSpeed", 6f);
            SetFloat(serialized, "defaultDrawSpeed", 2.5f);
            SetFloat(
                serialized,
                "maximumSegmentSampleDuration",
                0.75f);

            SetCurve(
                serialized,
                "widthByPressure",
                AnimationCurve.Linear(0f, 0.65f, 1f, 1.35f));

            SetCurve(
                serialized,
                "heightByPressure",
                AnimationCurve.Linear(0f, 0.70f, 1f, 1.25f));

            SetCurve(
                serialized,
                "pigmentByPressure",
                AnimationCurve.Linear(0f, 0.70f, 1f, 1.40f));

            SetCurve(
                serialized,
                "cutResistanceByPressure",
                AnimationCurve.Linear(0f, 0.60f, 1f, 1.60f));

            SetCurve(
                serialized,
                "lifecycleByPressure",
                AnimationCurve.Linear(0f, 0.75f, 1f, 1.25f));

            SetCurve(
                serialized,
                "budgetByPressure",
                AnimationCurve.Linear(0f, 0.80f, 1f, 1.25f));

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        private static RuntimeReferences ConfigurePaintRuntime(
            Type trackerType,
            ScriptableObject configAsset)
        {
            GameObject paintRuntime =
                FindSceneGameObject("PaintRuntime");

            if (paintRuntime == null)
            {
                throw new InvalidOperationException(
                    "Hierarchy'de PaintRuntime bulunamadı.");
            }

            Component tracker =
                GetOrAddComponent(
                    paintRuntime,
                    trackerType);

            Component brushController =
                FindComponentOnGameObject(
                    paintRuntime,
                    "PainterBrushController");

            if (brushController == null)
            {
                throw new InvalidOperationException(
                    "PaintRuntime üzerinde PainterBrushController bulunamadı.");
            }

            AssignReference(
                tracker,
                "config",
                configAsset,
                true);

            AssignReference(
                brushController,
                "pressureTracker",
                tracker,
                true);

            EditorUtility.SetDirty(paintRuntime);

            return new RuntimeReferences
            {
                PaintRuntime = paintRuntime,
                Tracker = tracker,
                BrushController = brushController
            };
        }

        private static Component ConfigurePressureHud(
            Type hudType,
            RuntimeReferences runtime)
        {
            GameObject canvasObject =
                FindSceneGameObject("Canvas");

            if (canvasObject == null ||
                canvasObject.GetComponent<Canvas>() == null)
            {
                throw new InvalidOperationException(
                    "Aktif sahnede Canvas isimli Canvas bulunamadı.");
            }

            Transform painterHud =
                FindChildRecursive(
                    canvasObject.transform,
                    "PainterHUD");

            if (painterHud == null)
            {
                throw new InvalidOperationException(
                    "Canvas altında PainterHUD bulunamadı.");
            }

            GameObject panel =
                GetOrCreateUiObject(
                    painterHud,
                    "StrokePressurePanel",
                    typeof(CanvasGroup));

            ConfigureTopLeftRect(
                panel.GetComponent<RectTransform>(),
                new Vector2(24f, -180f),
                new Vector2(340f, 65f));

            CanvasGroup canvasGroup =
                panel.GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = false;

            panel.SetActive(true);

            RemoveDirectChild(panel.transform, "StyleText");
            RemoveDirectChild(panel.transform, "SpeedText");
            RemoveDirectChild(panel.transform, "PressureSlider");

            Text styleText =
                CreateLegacyText(
                    panel.transform,
                    "StyleText",
                    "ÇİZİM BASINCI —",
                    13,
                    FontStyle.Bold,
                    new Vector2(0f, -2f),
                    new Vector2(330f, 20f));

            Text speedText =
                CreateLegacyText(
                    panel.transform,
                    "SpeedText",
                    string.Empty,
                    11,
                    FontStyle.Normal,
                    new Vector2(0f, -23f),
                    new Vector2(330f, 18f));

            Slider pressureSlider =
                CreateSlider(
                    panel.transform,
                    "PressureSlider",
                    new Vector2(0f, -48f),
                    new Vector2(220f, 7f),
                    0.5f,
                    "#211C22",
                    "#A62A3A");

            Component hud =
                GetOrAddComponent(
                    panel,
                    hudType);

            AssignReference(
                hud,
                "brushController",
                runtime.BrushController,
                true);

            AssignReference(
                hud,
                "pressureTracker",
                runtime.Tracker,
                true);

            AssignReference(
                hud,
                "canvasGroup",
                canvasGroup,
                true);

            AssignReference(
                hud,
                "pressureSlider",
                pressureSlider,
                true);

            AssignReference(
                hud,
                "styleText",
                styleText,
                true);

            AssignReference(
                hud,
                "speedText",
                speedText,
                true);

            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);

            return hud;
        }

        private static void AddToPainterBehaviours(
            Component hud)
        {
            if (hud == null)
                return;

            Component roleSwitcher =
                FindSceneComponent(
                    "PrototypeRoleSwitcher");

            if (roleSwitcher == null)
            {
                throw new InvalidOperationException(
                    "PrototypeRoleSwitcher bulunamadı.");
            }

            SerializedObject serialized =
                new SerializedObject(roleSwitcher);

            SerializedProperty array =
                serialized.FindProperty("painterBehaviours");

            if (array == null || !array.isArray)
            {
                throw new InvalidOperationException(
                    "PrototypeRoleSwitcher içinde painterBehaviours dizisi bulunamadı.");
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i)
                        .objectReferenceValue == hud)
                {
                    return;
                }
            }

            Undo.RecordObject(
                roleSwitcher,
                "Add Pressure HUD To Painter Behaviours");

            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            array.GetArrayElementAtIndex(index)
                .objectReferenceValue = hud;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(roleSwitcher);
        }

        private static void CreatePressureTestMarkers()
        {
            GameObject root =
                FindSceneGameObject("PressureTestMarkers");

            if (root == null)
            {
                root = new GameObject("PressureTestMarkers");
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create Pressure Test Markers");
            }

            Vector3 center = Vector3.zero;
            float surfaceY = 0.03f;

            GameObject ground =
                FindSceneGameObject("Ground");

            if (ground != null)
            {
                Renderer renderer =
                    ground.GetComponentInChildren<Renderer>();

                Collider collider =
                    ground.GetComponentInChildren<Collider>();

                if (renderer != null)
                {
                    center = renderer.bounds.center;
                    surfaceY = renderer.bounds.max.y + 0.025f;
                }
                else if (collider != null)
                {
                    center = collider.bounds.center;
                    surfaceY = collider.bounds.max.y + 0.025f;
                }
                else
                {
                    center = ground.transform.position;
                    surfaceY = center.y + 0.025f;
                }
            }

            Vector3 startPosition =
                new Vector3(
                    center.x - 2f,
                    surfaceY,
                    center.z);

            Vector3 endPosition =
                new Vector3(
                    center.x + 2f,
                    surfaceY,
                    center.z);

            ConfigureMarker(
                root.transform,
                "PressureTest_Start",
                startPosition);

            ConfigureMarker(
                root.transform,
                "PressureTest_End",
                endPosition);
        }

        private static void ConfigureMarker(
            Transform parent,
            string markerName,
            Vector3 worldPosition)
        {
            Transform existing =
                parent.Find(markerName);

            GameObject marker;

            if (existing == null)
            {
                marker =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                marker.name = markerName;
                Undo.RegisterCreatedObjectUndo(
                    marker,
                    "Create " + markerName);

                marker.transform.SetParent(
                    parent,
                    true);
            }
            else
            {
                marker = existing.gameObject;
            }

            Collider collider =
                marker.GetComponent<Collider>();

            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider);
            }

            marker.transform.position = worldPosition;
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale =
                new Vector3(0.18f, 0.03f, 0.18f);

            marker.SetActive(true);
            EditorUtility.SetDirty(marker);
        }

        private static Slider CreateSlider(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size,
            float initialValue,
            string backgroundHex,
            string fillHex)
        {
            GameObject sliderObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Slider));

            Undo.RegisterCreatedObjectUndo(
                sliderObject,
                "Create " + objectName);

            sliderObject.transform.SetParent(
                parent,
                false);

            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();

            ConfigureTopLeftRect(
                sliderRect,
                position,
                size);

            Image background =
                CreateSliderImage(
                    sliderRect,
                    "Background",
                    backgroundHex,
                    true);

            GameObject fillAreaObject =
                new GameObject(
                    "Fill Area",
                    typeof(RectTransform));

            Undo.RegisterCreatedObjectUndo(
                fillAreaObject,
                "Create Slider Fill Area");

            fillAreaObject.transform.SetParent(
                sliderRect,
                false);

            RectTransform fillArea =
                fillAreaObject.GetComponent<RectTransform>();

            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(1f, 1f);
            fillArea.offsetMax = new Vector2(-1f, -1f);

            Image fill =
                CreateSliderImage(
                    fillArea,
                    "Fill",
                    fillHex,
                    false);

            fill.rectTransform.pivot =
                new Vector2(0f, 0.5f);

            Slider slider =
                sliderObject.GetComponent<Slider>();

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(initialValue);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = null;
            slider.targetGraphic = null;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;

            background.raycastTarget = false;
            fill.raycastTarget = false;

            return slider;
        }

        private static Image CreateSliderImage(
            RectTransform parent,
            string objectName,
            string colorHex,
            bool background)
        {
            GameObject imageObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                imageObject,
                "Create " + objectName);

            imageObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                imageObject.GetComponent<RectTransform>();

            Stretch(rect);

            Image image =
                imageObject.GetComponent<Image>();

            image.color = ParseColor(colorHex);

            string resourcePath = background
                ? "UI/Skin/Background.psd"
                : "UI/Skin/UISprite.psd";

            Sprite sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    resourcePath);

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        private static Text CreateLegacyText(
            Transform parent,
            string objectName,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create " + objectName);

            textObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                textObject.GetComponent<RectTransform>();

            ConfigureTopLeftRect(
                rect,
                position,
                size);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = LoadLegacyFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return text;
        }

        private static GameObject GetOrCreateUiObject(
            Transform parent,
            string objectName,
            params Type[] extraComponents)
        {
            Transform existing = parent.Find(objectName);

            GameObject gameObject;

            if (existing != null)
            {
                gameObject = existing.gameObject;
            }
            else
            {
                List<Type> types = new List<Type>
                {
                    typeof(RectTransform)
                };

                types.AddRange(extraComponents);

                gameObject =
                    new GameObject(
                        objectName,
                        types.ToArray());

                Undo.RegisterCreatedObjectUndo(
                    gameObject,
                    "Create " + objectName);

                gameObject.transform.SetParent(
                    parent,
                    false);
            }

            foreach (Type componentType in extraComponents)
            {
                if (gameObject.GetComponent(componentType) == null)
                {
                    Undo.AddComponent(
                        gameObject,
                        componentType);
                }
            }

            return gameObject;
        }

        private static void ConfigureTopLeftRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void RemoveDirectChild(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);

            if (child != null)
            {
                Undo.DestroyObjectImmediate(
                    child.gameObject);
            }
        }

        private static Font LoadLegacyFont()
        {
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            if (font != null)
                return font;

            return Resources.GetBuiltinResource<Font>(
                "Arial.ttf");
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(
                hex,
                out Color color)
                ? color
                : Color.white;
        }

        private static void SetFloat(
            SerializedObject serialized,
            string propertyName,
            float value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    serialized.targetObject.GetType().Name +
                    " içinde " + propertyName + " alanı bulunamadı.");
            }

            property.floatValue = value;
        }

        private static void SetCurve(
            SerializedObject serialized,
            string propertyName,
            AnimationCurve value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    serialized.targetObject.GetType().Name +
                    " içinde " + propertyName + " alanı bulunamadı.");
            }

            property.animationCurveValue = value;
        }

        private static void AssignReference(
            Component component,
            string propertyName,
            UnityEngine.Object value,
            bool required)
        {
            if (component == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(
                        propertyName + " atanacak component null.");
                }

                return;
            }

            SerializedObject serialized =
                new SerializedObject(component);

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(
                        component.GetType().Name +
                        " içinde " + propertyName + " alanı bulunamadı.");
                }

                return;
            }

            Undo.RecordObject(
                component,
                "Assign " + propertyName);

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);
        }

        private static Component GetOrAddComponent(
            GameObject gameObject,
            Type type)
        {
            Component component =
                gameObject.GetComponent(type);

            if (component != null)
                return component;

            return Undo.AddComponent(gameObject, type);
        }

        private static Component FindComponentOnGameObject(
            GameObject gameObject,
            string typeName)
        {
            Type type =
                FindTypeDerivedFrom<MonoBehaviour>(typeName);

            return type != null
                ? gameObject.GetComponent(type)
                : null;
        }

        private static Component FindSceneComponent(
            string typeName)
        {
            Type type =
                FindTypeDerivedFrom<MonoBehaviour>(typeName);

            if (type == null)
                return null;

            UnityEngine.Object[] objects =
                Resources.FindObjectsOfTypeAll(type);

            foreach (UnityEngine.Object item in objects)
            {
                Component component = item as Component;

                if (component == null ||
                    !component.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                return component;
            }

            return null;
        }

        private static Type FindTypeDerivedFrom<T>(
            string typeName)
        {
            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<T>())
            {
                if (!type.IsAbstract &&
                    string.Equals(
                        type.Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return null;
        }

        private static GameObject FindSceneGameObject(
            string objectName)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform transform in transforms)
            {
                if (transform == null ||
                    !transform.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(transform))
                {
                    continue;
                }

                if (transform.name == objectName)
                    return transform.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform parent,
            string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform nested =
                    FindChildRecursive(child, childName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static string DecodeSource(string base64)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(base64));
        }

        private static string ReadNormalized(string assetPath)
        {
            return File.ReadAllText(assetPath)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        private static void WriteNormalized(
            string assetPath,
            string content)
        {
            string directory = Path.GetDirectoryName(assetPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                assetPath,
                content.Replace("\r\n", "\n")
                    .Replace("\r", "\n"),
                new UTF8Encoding(false));
        }

        private static void ValidateRequiredFile(
            string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                throw new FileNotFoundException(
                    "Gerekli dosya bulunamadı.",
                    assetPath);
            }
        }

        private static string CreateBackupDirectory()
        {
            string directory = Path.Combine(
                "Library",
                "PaintedAliveBackups",
                "StrokePressure_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(directory);
            return directory.Replace('\\', '/');
        }

        private static void BackupFile(
            string assetPath,
            string backupDirectory)
        {
            if (!File.Exists(assetPath))
                return;

            string safeName =
                assetPath.Replace('/', '_')
                    .Replace('\\', '_');

            string destination =
                Path.Combine(backupDirectory, safeName);

            File.Copy(
                assetPath,
                destination,
                true);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');

            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new ArgumentException(
                    "Asset klasörü Assets ile başlamalıdır.");
            }

            string current = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[i]);
                }

                current = next;
            }
        }

        private static string ReplaceRegexRequired(
            string input,
            string pattern,
            MatchEvaluator evaluator,
            string description)
        {
            Regex regex = new Regex(
                pattern,
                RegexOptions.Singleline);

            Match match = regex.Match(input);

            if (!match.Success)
            {
                throw new InvalidOperationException(
                    description +
                    " için hedef kod bulunamadı. Dosyalara yazılmadı.");
            }

            return regex.Replace(
                input,
                evaluator,
                1);
        }

        private static string ReplaceOptional(
            string input,
            string pattern,
            string replacement)
        {
            Regex regex = new Regex(
                pattern,
                RegexOptions.Singleline);

            return regex.IsMatch(input)
                ? regex.Replace(input, replacement, 1)
                : input;
        }

        private static string ReplaceMethodRequired(
            string input,
            string signaturePattern,
            string replacement,
            string description)
        {
            MethodSpan span =
                FindMethodSpan(
                    input,
                    signaturePattern,
                    description);

            return input.Substring(0, span.Start) +
                   replacement +
                   input.Substring(span.EndExclusive);
        }

        private static string InsertAfterMethodRequired(
            string input,
            string signaturePattern,
            string addition,
            string description)
        {
            MethodSpan span =
                FindMethodSpan(
                    input,
                    signaturePattern,
                    description);

            return input.Insert(
                span.EndExclusive,
                addition);
        }

        private static string InsertAfterMemberRequired(
            string input,
            string signaturePattern,
            string addition,
            string description)
        {
            Match signature = Regex.Match(
                input,
                signaturePattern,
                RegexOptions.Singleline);

            if (!signature.Success)
            {
                throw new InvalidOperationException(
                    description +
                    " üyesi bulunamadı. Dosyalara yazılmadı.");
            }

            int cursor = signature.Index + signature.Length;

            while (cursor < input.Length &&
                   char.IsWhiteSpace(input[cursor]))
            {
                cursor++;
            }

            if (cursor + 1 < input.Length &&
                input[cursor] == '=' &&
                input[cursor + 1] == '>')
            {
                int semicolon = input.IndexOf(';', cursor + 2);

                if (semicolon < 0)
                {
                    throw new InvalidOperationException(
                        description +
                        " expression-bodied bitişi bulunamadı.");
                }

                return input.Insert(
                    semicolon + 1,
                    addition);
            }

            MethodSpan span =
                FindMethodSpan(
                    input,
                    signaturePattern,
                    description);

            return input.Insert(
                span.EndExclusive,
                addition);
        }

        private static string TransformMethodRequired(
            string input,
            string signaturePattern,
            Func<string, string> transform,
            string description)
        {
            MethodSpan span =
                FindMethodSpan(
                    input,
                    signaturePattern,
                    description);

            string method = input.Substring(
                span.Start,
                span.EndExclusive - span.Start);

            string transformed = transform(method);

            return input.Substring(0, span.Start) +
                   transformed +
                   input.Substring(span.EndExclusive);
        }

        private static MethodSpan FindMethodSpan(
            string input,
            string signaturePattern,
            string description)
        {
            Match signature = Regex.Match(
                input,
                signaturePattern,
                RegexOptions.Singleline);

            if (!signature.Success)
            {
                throw new InvalidOperationException(
                    description +
                    " metodu bulunamadı. Dosyalara yazılmadı.");
            }

            int openBrace = input.IndexOf(
                '{',
                signature.Index + signature.Length);

            if (openBrace < 0)
            {
                throw new InvalidOperationException(
                    description +
                    " açılış süslü parantezi bulunamadı.");
            }

            int closeBrace = FindMatchingBrace(
                input,
                openBrace);

            if (closeBrace < 0)
            {
                throw new InvalidOperationException(
                    description +
                    " kapanış süslü parantezi bulunamadı.");
            }

            return new MethodSpan(
                signature.Index,
                closeBrace + 1);
        }

        private static int FindMatchingBrace(
            string input,
            int openBrace)
        {
            int depth = 0;
            bool inString = false;
            bool inChar = false;
            bool inLineComment = false;
            bool inBlockComment = false;
            bool escape = false;

            for (int i = openBrace; i < input.Length; i++)
            {
                char current = input[i];
                char next = i + 1 < input.Length
                    ? input[i + 1]
                    : '\0';

                if (inLineComment)
                {
                    if (current == '\n')
                        inLineComment = false;

                    continue;
                }

                if (inBlockComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (current == '"')
                        inString = false;

                    continue;
                }

                if (inChar)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (current == '\'')
                        inChar = false;

                    continue;
                }

                if (current == '/' && next == '/')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (current == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    continue;
                }

                if (current == '\'')
                {
                    inChar = true;
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;

                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private readonly struct MethodSpan
        {
            public MethodSpan(
                int start,
                int endExclusive)
            {
                Start = start;
                EndExclusive = endExclusive;
            }

            public int Start { get; }
            public int EndExclusive { get; }
        }

        private sealed class RuntimeReferences
        {
            public GameObject PaintRuntime;
            public Component Tracker;
            public Component BrushController;
        }
    }
}
