using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StereoKitApp.Painting;
using StereoKit;
using StereoKitApp.Utils.Colors;

// ReSharper disable once EmptyNamespace
namespace StereoKitApp.Tests.Painting
{
    [TestFixture]
    public class Box3DModelDataTests
    {
        private Box3DModelData _sut = null!;

        private readonly Box3DModelData.PixelData[] _seedPixelDataHistory = new[]
        {
            new Box3DModelData.PixelData()
            {
                Position = Vec3.Forward,
                Color = Color.Black,
                PixelStatus = Box3DModelData.PixelStatus.Visible
            },
            new Box3DModelData.PixelData()
            {
                Position = Vec3.Forward,
                Color = Colors.Magenta,
                PixelStatus = Box3DModelData.PixelStatus.Deleted
            },
            new Box3DModelData.PixelData()
            {
                Position = Vec3.Up,
                Color = Color.White,
                PixelStatus = Box3DModelData.PixelStatus.Visible
            },
            new Box3DModelData.PixelData()
            {
                Position = Vec3.Up,
                Color = Color.Black,
                PixelStatus = Box3DModelData.PixelStatus.Visible
            }
        };

        [SetUp]
        public void Setup()
        {
            _sut = new Box3DModelData();
        }

        [Test]
        public void WhenAddingEdit_TheOutputsIgnoreDeletedStuff()
        {
            var pix1 = new Box3DModelData.PixelData()
            {
                Color = Color.White,
                Position = new Vec3(0, 0, 1),
                PixelStatus = Box3DModelData.PixelStatus.Visible
            };

            _sut.AddPixelEdit(pix1);

            Assert.That(_sut.ActivePixelDatas, Has.Exactly(1).Items);

            var pix1Clone = pix1.Clone();
            pix1Clone.PixelStatus = Box3DModelData.PixelStatus.Deleted;
            _sut.AddPixelEdit(pix1Clone);

            Assert.That(_sut.ActivePixelDatas, Is.Empty);
        }

        [Test]
        public void WhenAddingEditIfEditIsIdenticalToLastEdit_IgnoreIt()
        {
            _sut.CreateOrUpdatePixel(Vec3.One, Color.White, 0, Quat.Identity);

            Assert.That(_sut.GetPixelEditHistorySequence(), Has.One.Items);

            _sut.CreateOrUpdatePixel(Vec3.One, Color.White, 0, Quat.Identity);
            Assert.That(_sut.GetPixelEditHistorySequence(), Has.One.Items);

            _sut.CreateOrUpdatePixel(Vec3.One, Color.Black, 0, Quat.Identity);
            Assert.That(_sut.GetPixelEditHistorySequence(), Has.Exactly(2).Items);
        }

        [Test]
        public void LastVisualItemFromHistoryOfPixelIsShown()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);

            var data = _sut.CalculateVisiblePixelsAtPointInHistory(3).ToArray();

            // Expecting
            // 0: Create A
            // 1: Delete A
            // 2: Create B
            // = Only B

            Assert.That(data, Has.Exactly(1).Items);
            var item = data[0];
            Assert.That(item.Color, Is.EqualTo(Color.White)); // We change color in next step
        }

        [Test]
        public void ClearChanges_ClearsAsManyAsInput()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);
            Assert.That(_sut.TotalChanges, Is.EqualTo(4));
            _sut.ClearChanges(1);
            Assert.That(_sut.TotalChanges, Is.EqualTo(3));
        }

        [Test]
        public void GetActivePixel_ReturnsNullWhenThePixelHasBeenDeleted()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);
            var activePixel = _sut.GetActivePixelAtPosition(Vec3.Forward);
            Assert.That(activePixel, Is.Null);
        }

        [Test]
        public void ClearChanges_WhenOutOfRange_Succeeds()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);
            Assert.That(_sut.TotalChanges, Is.EqualTo(4));
            _sut.ClearChanges();
            Assert.That(_sut.TotalChanges, Is.EqualTo(0));
        }

        [Test]
        public void ClearChanges_WhenNegative_Succeeds()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);
            Assert.That(_sut.TotalChanges, Is.EqualTo(4));
            _sut.ClearChanges(-1);
            Assert.That(_sut.TotalChanges, Is.EqualTo(4));
        }

        [Test]
        public void SerializeAndDeserializeIsReversable()
        {
            InitializeWithDataset(ref _sut, _seedPixelDataHistory);

            var serialized1 = _sut.Serialize();

            var sut2 = Box3DModelData.Deserialize(serialized1);

            var serialized2 = sut2.Serialize();

            Assert.That(serialized1, Is.EqualTo(serialized2));
        }

        private void InitializeWithDataset(
            ref Box3DModelData box3DModelData,
            IReadOnlyList<Box3DModelData.PixelData> data
        )
        {
            foreach (var pixelData in data)
            {
                _sut.AddPixelEdit(pixelData);
            }
        }
    }
}
