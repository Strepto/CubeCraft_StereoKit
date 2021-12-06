using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using StereoKit;
using StereoKitApp.HLRuffles;
using StereoKitApp.Utils;

namespace StereoKitApp.UIs
{
    public class ConnectionMenu
    {
        public string CurrentIpInput { get; private set; } = "127.0.0.1";

        public Action<string>? PressedConnect;

        private Pose _pose;
        private readonly Sprite _rocket;

        private readonly List<string> _suggestedIps = new List<string>
        {
            "127.0.0.1",
            "192.168.0.",
            "192.168.1.232"
        };

        public ConnectionMenu(ref Pose pose)
        {
            _pose = pose;
            _rocket = AssetLookup.Sprites.LoadRocketSprite;
            PressedConnect += ip =>
            {
                if (!_suggestedIps.Contains(ip))
                {
                    _suggestedIps.Insert(0, ip);
                }
            };
        }

        public void Update()
        {
            using (
                UIUtils.UIWindowScope(
                    "Connection",
                    ref _pose,
                    Vec2.Zero,
                    UIWin.Normal,
                    UIMove.FaceUser
                )
            )
            {
                IpInputUpdate();
            }
        }

        // ReSharper disable once CognitiveComplexity -- Its complex yeah.
        private void IpInputUpdate()
        {
            UI.Text(CurrentIpInput);

            UI.Text("");

            if (UI.Button("<-"))
            {
                // Clear input if there is any "error message" or similar
                if (Regex.IsMatch(CurrentIpInput, "[a-z]+", RegexOptions.IgnoreCase))
                {
                    CurrentIpInput = "";
                }
                else
                {
                    CurrentIpInput = CurrentIpInput.Substring(0, CurrentIpInput.Length - 1);
                }
            }

            if (Numpad(out var clickedNumber))
            {
                if (clickedNumber.TryGetNumber(out int clickedNum))
                    CurrentIpInput += clickedNum.ToString();
                else if (clickedNumber == NumpadValue.Dot)
                {
                    CurrentIpInput += ".";
                }
                else if (clickedNumber == NumpadValue.Submit)
                {
                    RufflesTransport.Singleton.JoinSession(
                        new IPEndPoint(IPAddress.Parse(CurrentIpInput), 6776)
                    );
                    PressedConnect?.Invoke(CurrentIpInput);
                }
                else
                    throw new ArgumentOutOfRangeException(
                        nameof(clickedNumber),
                        clickedNumber.ToString()
                    );

                // "Smart" ip segmenting.
                var segments = CurrentIpInput.Trim().Split('.');
                if (segments.Length < 4 && segments.Last().Length == 3)
                {
                    CurrentIpInput += ".";
                }
            }

            UI.HSeparator();

            foreach (var suggestedIp in _suggestedIps)
            {
                if (UI.Button(suggestedIp))
                {
                    CurrentIpInput = suggestedIp;
                }
            }

            if (UI.Button("Create session"))
            {
                RufflesTransport.Singleton.CreateSession(6776);
            }
        }

        /// <summary>
        /// Simple numpad that outputs the number clicked if it returns true.
        /// </summary>
        /// <param name="clickedValue"></param>
        /// <returns></returns>
        private bool Numpad(out NumpadValue clickedValue)
        {
            clickedValue = NumpadValue.Err;
            for (int i = 1; i <= 9; i++)
            {
                if (UI.Button(i.ToString()))
                {
                    clickedValue = (NumpadValue)i;
                }

                if (i % 3 != 0)
                {
                    UI.SameLine();
                }
            }

            UI.NextLine();

            if (UI.Button(" ."))
            {
                clickedValue = NumpadValue.Dot;
            }

            UI.SameLine();
            if (UI.Button("0"))
            {
                clickedValue = NumpadValue.Num0;
            }

            UI.SameLine();
            if (UI.ButtonRound("Enter", _rocket))
            {
                clickedValue = NumpadValue.Submit;
            }

            return clickedValue != NumpadValue.Err;
        }
    }
}
