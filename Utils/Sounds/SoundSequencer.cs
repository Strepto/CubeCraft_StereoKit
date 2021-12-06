using System;
using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Utils.Sounds
{
    public class SoundSequencer
    {
        private readonly Random _randomNumberGenerator = new Random();
        private IReadOnlyList<Sound> SoundSequence { get; }
        private LoopingMode SequenceLoopingMode { get; }

        private int _nextSoundIndex = 0;

        private Direction _direction = Direction.Up;

        private enum Direction
        {
            Down = -1,
            Up = 1
        }

        public enum LoopingMode
        {
            /// <summary>
            /// When the sequence ends, restarts from beginning
            /// </summary>
            Restart,
            /// <summary>
            /// When the sequence ends, continues in reverse from the end
            /// </summary>
            PingPong,
            /// <summary>
            /// Each next sound is random. Sounds can be repeated.
            /// </summary>
            TrueRandom
        }

        /// <summary>
        /// Makes a Sequencer that helps with sound sequences. If you want a different sound to play each time you do an action.
        /// </summary>
        /// <param name="soundSequence">The sequence to play</param>
        /// <param name="loopingMode"></param>
        public SoundSequencer(IReadOnlyList<Sound> soundSequence, LoopingMode loopingMode)
        {
            SequenceLoopingMode = loopingMode;
            SoundSequence = soundSequence;
            ResetToFirst();
        }

        public void ResetToFirst()
        {
            _nextSoundIndex = 0;
            _direction = Direction.Up;
        }

        public void PlayNextSoundInSequence(Vec3 globalPosition, float volume = 1f)
        {
            SoundSequence[_nextSoundIndex].Play(globalPosition, volume);

            SelectNextSoundIndex();
        }

        private void SelectNextSoundIndex()
        {
            if (SequenceLoopingMode == LoopingMode.TrueRandom)
            {
                _nextSoundIndex = _randomNumberGenerator.Next(0, SoundSequence.Count - 1);
            }

            var next = _nextSoundIndex + (int)_direction;
            if (next >= SoundSequence.Count || next < 0)
            {
                switch (SequenceLoopingMode)
                {
                    case LoopingMode.Restart:
                        _nextSoundIndex = 0;
                        break;
                    case LoopingMode.PingPong:
                        _direction = _direction == Direction.Up ? Direction.Down : Direction.Up;
                        _nextSoundIndex += (int)_direction;
                        break;
                    // ReSharper disable once RedundantCaseLabel -- TrueRandom should not happen
                    case LoopingMode.TrueRandom:
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(SequenceLoopingMode),
                            SequenceLoopingMode.ToString()
                        );
                }
            }
            else
            {
                _nextSoundIndex = next;
            }
        }
    }
}
