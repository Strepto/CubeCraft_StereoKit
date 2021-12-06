using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using StereoKit;
using StereoKitApp.Utils.EqualityComparers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StereoKitApp.Utils.Colors;

namespace StereoKitApp.Painting
{
    /// <summary>
    /// Contains the data to represent a Box3DDrawing.
    ///
    /// Whenever the data changes it will emit events.
    ///
    /// It has helpers to serialize and deserialize the data.
    /// </summary>
    public class Box3DModelData
    {
        public enum PixelStatus : byte
        {
            None = 0,
            Visible = 1,
            Deleted = 2
        }

        public enum VoxelKind : byte
        {
            Cube = 0,
            Rounded = 1,
            RoundedEdge = 2,
            Sliced = 3,
            RoundedTop = 4,
            Half = 5,
            Chipped = 6,
            CutEdge = 7,
            CutTop = 8
        }

        public struct PixelData
        {
            public class Vec3JsonConverter : JsonConverter<Vec3>
            {
                public override void WriteJson(
                    JsonWriter writer,
                    Vec3 value,
                    JsonSerializer serializer
                )
                {
                    writer.WriteRawValue(JsonConvert.SerializeObject(value.v));
                }

                public override Vec3 ReadJson(
                    JsonReader reader,
                    Type objectType,
                    Vec3 existingValue,
                    bool hasExistingValue,
                    JsonSerializer serializer
                )
                {
                    var jObject = JObject.Load(reader);
                    var json = jObject.ToString();
                    return JsonConvert.DeserializeObject<Vector3>(json);
                }
            }

            public class QuatJsonConverter : JsonConverter<Quat>
            {
                public override void WriteJson(
                    JsonWriter writer,
                    Quat value,
                    JsonSerializer serializer
                )
                {
                    writer.WriteRawValue(JsonConvert.SerializeObject(value.q));
                }

                public override Quat ReadJson(
                    JsonReader reader,
                    Type objectType,
                    Quat existingValue,
                    bool hasExistingValue,
                    JsonSerializer serializer
                )
                {
                    var jObject = JObject.Load(reader);
                    var json = jObject.ToString();
                    return JsonConvert.DeserializeObject<Quaternion>(json);
                }
            }

            public PixelStatus PixelStatus;
            public Vec3 Position;
            public Color Color;
            public VoxelKind VoxelKind;
            public Quat CubeRotation;

            public PixelData Clone()
            {
                return new PixelData()
                {
                    PixelStatus = PixelStatus,
                    Position = Position,
                    Color = Color,
                    VoxelKind = VoxelKind,
                    CubeRotation = CubeRotation
                };
            }
        }

        private class PixelEditHistory
        {
            private class PixelDataCache
            {
                public PixelDataCache(IReadOnlyList<PixelData> initialData)
                {
                    _activePixelDatas = initialData.ToList();
                    _boundsCache = CalculateBounds();
                }

                private Bounds CalculateBounds()
                {
                    if (!_activePixelDatas.Any())
                        return new Bounds();

                    var positions = _activePixelDatas.Select(x => x.Position).ToArray();
                    var maxBounds = positions.Aggregate(Vec3.Max);
                    var minBounds = positions.Aggregate(Vec3.Min);

                    var half = Vec3.One * 0.5f;

                    var bounds = Bounds.FromCorners(minBounds - half, maxBounds + half);
                    Debug.Assert(bounds.dimensions.x >= 0);
                    Debug.Assert(bounds.dimensions.y >= 0);
                    Debug.Assert(bounds.dimensions.z >= 0);
                    return bounds;
                }

                private readonly List<PixelData> _activePixelDatas;
                public IReadOnlyList<PixelData> ActivePixelDatas => _activePixelDatas;
                private Bounds _boundsCache;
                public Bounds BoundsCache => _boundsCache;

                public void PixelChange(PixelData? oldPixelData, PixelData newPixelData)
                {
                    if (oldPixelData.HasValue)
                        _activePixelDatas.Remove(oldPixelData.Value);
                    if (newPixelData.PixelStatus == PixelStatus.Visible)
                    {
                        _activePixelDatas.Add(newPixelData);
                    }
                    _boundsCache = CalculateBounds();
                }
            }

            /// <summary>
            /// This events happens if the user.
            /// </summary>
            public Action<IReadOnlyList<PixelEdit>>? BreakingHistoryChange = null;
            public Action<PixelEdit>? NewChange = null;

            /// <summary>
            /// Get the Pixel Data for a point in history.
            /// </summary>
            /// <param name="changes">How many changes should we include? 0 Is none. 3 is three changes.</param>
            /// <returns>An enumerable list of changes.</returns>
            // ReSharper disable once CognitiveComplexity -- Complexity covered by tests
            public IEnumerable<PixelData> CalculateVisiblePixelsAtPointInHistory(int changes)
            {
                var editSubset = _intPixelEdits.Take(changes).GroupBy(x => x.Key);

                foreach (var pixelEditsByPixel in editSubset)
                {
                    foreach (var pixelEdit in pixelEditsByPixel.Reverse())
                    {
                        if (pixelEdit.PixelData.PixelStatus == PixelStatus.Visible)
                        {
                            yield return pixelEdit.PixelData;
                            break;
                        }

                        if (pixelEdit.PixelData.PixelStatus == PixelStatus.Deleted)
                            break; // The pixel has been removed.
                    }
                }
            }

            private List<PixelEdit> _intPixelEdits;
            private PixelDataCache _pixelDataCache;

            public IReadOnlyList<PixelEdit> PixelEdits => _intPixelEdits;

            public IReadOnlyList<PixelData> ActivePixelDatasCached =>
                _pixelDataCache.ActivePixelDatas;
            public Bounds ActivePixelDatasBoundsCached => _pixelDataCache.BoundsCache;

            public PixelEditHistory(IReadOnlyList<PixelEdit>? initial = null)
            {
                _intPixelEdits = initial?.ToList() ?? new List<PixelEdit>();
                var visibleData = CalculateVisiblePixelsAtPointInHistory(int.MaxValue).ToList();
                _pixelDataCache = new PixelDataCache(visibleData);
            }

            /// <summary>
            /// Adds a new Edit to the given Pixel Data.
            /// If the last edit of this pixel is identical to the new one the edit will be discarded.
            /// </summary>
            /// <param name="newPixelData"></param>
            public void AddPixelEdit(PixelData newPixelData, bool addSilent = false)
            {
                var lastEditOfPixel = FindLastEditOfPixel(newPixelData.Position);

                if (lastEditOfPixel != null && lastEditOfPixel.PixelData.Equals(newPixelData))
                    return;

                var edit = new PixelEdit(newPixelData, DateTimeOffset.Now);
                _intPixelEdits.Add(new PixelEdit(newPixelData, DateTimeOffset.Now));

                _pixelDataCache.PixelChange(lastEditOfPixel?.PixelData, newPixelData);

                if (addSilent)
                    return;

                NewChange?.Invoke(edit);
            }

            public int TotalChanges => _intPixelEdits.Count;

            /// <summary>
            /// This will irreversibly clear X changes from the history.
            /// Can be used to restart from a point in history.
            /// </summary>
            /// <param name="countOfChangesToRemove"></param>
            public void ClearChanges(int countOfChangesToRemove = int.MaxValue)
            {
                if (countOfChangesToRemove < 1)
                    return;

                _intPixelEdits.RemoveRange(
                    Math.Max(0, _intPixelEdits.Count - countOfChangesToRemove),
                    Math.Min(countOfChangesToRemove, TotalChanges)
                );

                _pixelDataCache = new PixelDataCache(
                    CalculateVisiblePixelsAtPointInHistory(TotalChanges).ToArray()
                );

                BreakingHistoryChange?.Invoke(_intPixelEdits.ToArray());
            }

            private PixelEdit? FindLastEditOfPixel(Vec3 pixelPos)
            {
                for (var i = _intPixelEdits.Count - 1; i >= 0; i--)
                {
                    var edit = _intPixelEdits[i];

                    if (Vec3EqualityComparer.Instance.Equals(edit.PixelData.Position, pixelPos))
                    {
                        return edit;
                    }
                }

                return null;
            }
            public PixelData? GetActivePixelAtPosition(Vec3 pixelPos)
            {
                var lastEdit = FindLastEditOfPixel(pixelPos);
                if (lastEdit == null || lastEdit.PixelData.PixelStatus != PixelStatus.Visible)
                    return null;
                return FindLastEditOfPixel(pixelPos)?.PixelData;
            }
        }

        private readonly PixelEditHistory _pixelEditHistory;

        /// <summary>
        /// Get a reference to the active Voxels.
        /// This is a cached array, so its relatively fast :)
        /// </summary>
        public IReadOnlyList<PixelData> ActivePixelDatas =>
            _pixelEditHistory.ActivePixelDatasCached;

        public Bounds ActivePixelDatasBounds => _pixelEditHistory.ActivePixelDatasBoundsCached;

        public IEnumerable<PixelEdit> GetPixelEditHistorySequence()
        {
            return _pixelEditHistory.PixelEdits.ToArray();
        }

        public Action<IReadOnlyList<PixelEdit>>? Box3DModelDataNeedsResync = null;
        public Action<PixelEdit>? Box3DModelDataEdited = null;

        public Box3DModelData(IEnumerable<PixelEdit>? edits = null)
        {
            _pixelEditHistory = new PixelEditHistory(edits?.ToList());
            _pixelEditHistory.NewChange += edit => Box3DModelDataEdited?.Invoke(edit);
            _pixelEditHistory.BreakingHistoryChange += Box3DModelDataNeedsResync;
        }

        private IEnumerable<PixelEdit> FindAllEditsOfPixel(Vec3 pixelPoint)
        {
            return _pixelEditHistory.PixelEdits.Where(
                x => Vec3EqualityComparer.Instance.Equals(x.Key, pixelPoint)
            );
        }

        public class PixelEdit
        {
            public Vec3 Key => PixelData.Position;
            public PixelData PixelData;
            public DateTimeOffset EditTimestamp;

            public PixelEdit() { }

            public PixelEdit(PixelData pixelData, DateTimeOffset editTimestamp)
            {
                PixelData = pixelData;
                EditTimestamp = editTimestamp;
            }

            public PixelEdit DeepClone(bool updateTimestamp = true) =>
                new PixelEdit(
                    PixelData.Clone(),
                    updateTimestamp ? DateTimeOffset.UtcNow : EditTimestamp
                );
        }

        public void CreateOrUpdatePixel(
            Vec3 pixelPos,
            Color color,
            VoxelKind voxelKind,
            Quat cubeRotation
        )
        {
            var pixelData = new PixelData()
            {
                PixelStatus = PixelStatus.Visible,
                Color = color,
                Position = pixelPos,
                VoxelKind = voxelKind,
                CubeRotation = cubeRotation
            };

            _pixelEditHistory.AddPixelEdit(pixelData);
        }

        public void DeletePixel(Vec3 pixelPos)
        {
            var pixelData = new PixelData()
            {
                PixelStatus = PixelStatus.Deleted,
                Color = Colors.Magenta,
                Position = pixelPos,
            };

            _pixelEditHistory.AddPixelEdit(pixelData);
        }

        public PixelData? GetActivePixelAtPosition(Vec3 pixelPos)
        {
            return _pixelEditHistory.GetActivePixelAtPosition(pixelPos);
        }

        public void UndoLastEdit()
        {
            _pixelEditHistory.ClearChanges(1);
        }

        public int TotalChanges => _pixelEditHistory.TotalChanges;

        [JsonObject]
        private class SerializableBox3DModelData
        {
            public const int CurrentFileFormatVersion = 1; // Maybe support migrations in the future?
            public int Version { get; set; } = CurrentFileFormatVersion;
            public PixelEdit[] PixelEdits { get; set; } = null!;
        }

        public string Serialize()
        {
            var serializable = new SerializableBox3DModelData()
            {
                PixelEdits = _pixelEditHistory.PixelEdits.ToArray()
            };
            return JsonConvert.SerializeObject(
                serializable,
                new PixelData.Vec3JsonConverter(),
                new PixelData.QuatJsonConverter()
            );
        }

        public static Box3DModelData Deserialize(string jsonPixelEdits)
        {
            var data = JsonConvert.DeserializeObject<SerializableBox3DModelData>(
                jsonPixelEdits,
                new PixelData.Vec3JsonConverter(),
                new PixelData.QuatJsonConverter()
            );

            if (data!.Version != SerializableBox3DModelData.CurrentFileFormatVersion)
                Console.WriteLine(
                    $"Input format {data.Version} was different from current: {SerializableBox3DModelData.CurrentFileFormatVersion}"
                );

            var box = new Box3DModelData(data.PixelEdits);
            return box;
        }

        public void ClearChanges(int totalChanges = Int32.MaxValue)
        {
            _pixelEditHistory.ClearChanges(totalChanges);
        }

        public IEnumerable<PixelData> CalculateVisiblePixelsAtPointInHistory(int time)
        {
            return _pixelEditHistory.CalculateVisiblePixelsAtPointInHistory(time);
        }

        public void AddPixelEdit(PixelData pixelData, bool addSilent = false)
        {
            _pixelEditHistory.AddPixelEdit(pixelData, addSilent);
        }

        public void AddPixelEdits(IEnumerable<PixelData> pixelData)
        {
            // Remark: Consider implementing batching for this if its needed.
            foreach (var data in pixelData)
            {
                _pixelEditHistory.AddPixelEdit(data);
            }
        }
    }
}
