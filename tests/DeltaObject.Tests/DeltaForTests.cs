using DeltaObject;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace DeltaObjects.Tests
{
    public class DeltaForTests : IDisposable
    {
        public DeltaForTests()
        {
            DeltaObjectMappingConfig.ClearMappings();
        }

        public void Dispose()
        {
            DeltaObjectMappingConfig.ClearMappings();
        }

        [Fact]
        public void DeltaForObjectIsCreatedWithNoSetProperties()
        {
            var delta = new DeltaFor<TestObject>();

            delta.Property(o => o.Int1).IsSet.Should().BeFalse();
            delta.Property(o => o.String1).IsSet.Should().BeFalse();
            delta.Property(o => o.String2).IsSet.Should().BeFalse();
            delta.Property(o => o.DateTime1).IsSet.Should().BeFalse();
            delta.Property(o => o.Bool1).IsSet.Should().BeFalse();
        }

        [Fact]
        public void SettingPropertyValuesShouldMarkPropertiesAsSet()
        {
            var jsonString = "{ int1: 1, string1: 'val1', string2: 'val2' }";

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);

            delta.Property(o => o.Int1).IsSet.Should().BeTrue();
            delta.Property(o => o.Int1).Value.Should().Be(1);
            delta.Property(o => o.String1).IsSet.Should().BeTrue();
            delta.Property(o => o.String1).Value.Should().Be("val1");
            delta.Property(o => o.String2).IsSet.Should().BeTrue();
            delta.Property(o => o.String2).Value.Should().Be("val2");
            delta.Property(o => o.DateTime1).IsSet.Should().BeFalse();
            delta.Property(o => o.Bool1).IsSet.Should().BeFalse();
        }

        [Fact]
        public void SettingCollectionValueTest()
        {
            var jsonString = "{ intList1: [1, 3, 5] }";

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObjectWithArray>>(jsonString);

            delta.Property(o => o.IntList1).IsSet.Should().BeTrue();
            delta.Property(o => o.IntList1).Value.Should().BeEquivalentTo(new[] { 1, 3, 5 });
        }

        [Fact]
        public void SetValuesForComplexPropertiesTest()
        {
            var jsonString = "{ testObject1: { string1: 'val1', bool1: true } }";

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObjectWithComplexProperty>>(jsonString);

            delta.Property(o => o.TestObject1).IsSet.Should().BeTrue();
            delta.Property(o => o.TestObject1).Value.String1.Should().Be("val1");
            delta.Property(o => o.TestObject1).Value.Bool1.Should().Be(true);
        }

        [Fact]
        public void PatchObjectTest()
        {
            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04' }";
            var testObject = new TestObject();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(testObject);

            testObject.Int1.Should().Be(1);
            testObject.String1.Should().Be("val1");
            testObject.String2.Should().Be(default(string));
            testObject.DateTime1.Should().Be(new DateTime(2018, 12, 4));
            testObject.Bool1.Should().Be(default(bool));
        }

        [Fact]
        public void PatchInheritedObjectTest()
        {
            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04', bool2: true }";
            var subTestObject = new SubTestObject();

            var delta = JsonConvert.DeserializeObject<DeltaFor<SubTestObject>>(jsonString);
            delta.Patch(subTestObject);

            subTestObject.Int1.Should().Be(1);
            subTestObject.String1.Should().Be("val1");
            subTestObject.String2.Should().Be(default(string));
            subTestObject.DateTime1.Should().Be(new DateTime(2018, 12, 4));
            subTestObject.Bool1.Should().Be(default(bool));

            subTestObject.Int2.Should().Be(default(int));
            subTestObject.Bool2.Should().Be(true);
        }

        [Fact]
        public void PatchTargetWihDifferentType()
        {
            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04' }";
            var testObject = new TestObject();

            var targetObject = new TestObject2();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(targetObject);

            targetObject.Int1.Should().Be(1);
            targetObject.String2.Should().Be(default(string));
            targetObject.DateTime1.Should().Be(new DateTime(2018, 12, 4));
        }

        [Fact]
        public void PatchWithMappings()
        {
            DeltaObjectMappingConfig<TestObject, TestObject2>.GlobalConfig()
                .Map(p => p.Int1, t => t.Int2)
                .Map(p => p.String1, t => t.String2)
                .Map(p => p.Bool1, t => t.Bool3);

            var jsonString = "{ int1: 1, string1: 'val1', bool1: true }";
            var testObject = new TestObject();

            var targetObject = new TestObject2();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(targetObject);

            targetObject.Int2.Should().Be(1);
            targetObject.String2.Should().Be("val1");
            targetObject.Bool3.Should().Be(true);
        }

        [Fact]
        public void PatchIgnoringProperties()
        {
            DeltaObjectMappingConfig<TestObject, TestObject2>.GlobalConfig()
                .Map(p => p.String1, t => t.String2)
                .Ignore(p => p.Int1)
                .Ignore(p => p.Bool1)
                .Ignore(p => p.DateTime1);

            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04' }";
            var testObject = new TestObject();

            var targetObject = new TestObject2();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(targetObject);

            targetObject.Int1.Should().Be(default(int));
            targetObject.String2.Should().Be("val1");
            targetObject.DateTime1.Should().Be(default(DateTime));
        }

        [Fact]
        public void PatchIgnoringNonMappedProperties()
        {
            DeltaObjectMappingConfig<TestObject, TestObject>.GlobalConfig()
                .Map(p => p.DateTime1, t => t.DateTime1)
                .Map(p => p.Int1, t => t.Int1)
                .IgnoreNonMapped();

            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04' }";
            var testObject = new TestObject();

            var targetObject = new TestObject();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(targetObject);

            targetObject.Int1.Should().Be(1);
            targetObject.String1.Should().Be(default(string));
            targetObject.DateTime1.Should().Be(new DateTime(2018, 12, 4));
        }

        [Fact]
        public void PatchApplyingMappingFunction()
        {
            DeltaObjectMappingConfig<TestObject, TestObject>.GlobalConfig()
                .Map(p => p.DateTime1, t => t.DateTime1, (value) => value.AddDays(1))
                .Map(p => p.Int1, t => t.Int1, (value) => value + 2);

            var jsonString = "{ int1: 1, string1: 'val1', dateTime1: '2018-12-04' }";
            var testObject = new TestObject();

            var targetObject = new TestObject();

            var delta = JsonConvert.DeserializeObject<DeltaFor<TestObject>>(jsonString);
            delta.Patch(targetObject);

            targetObject.Int1.Should().Be(3);
            targetObject.String1.Should().Be("val1");
            targetObject.DateTime1.Should().Be(new DateTime(2018, 12, 5));
        }
    }

    public class TestObject
    {
        public int Int1 { get; set; }
        public string String1 { get; set; }
        public string String2 { get; set; }
        public DateTime DateTime1 { get; set; }
        public bool Bool1 { get; set; }
    }

    public class TestObject2
    {
        public int Int1 { get; set; }
        public int Int2 { get; set; }
        public string String2 { get; set; }
        public DateTime DateTime1 { get; set; }
        public bool Bool3 { get; set; }
    }

    public class TestObjectWithArray
    {
        public List<int> IntList1 { get; set; }
    }

    public class TestObjectWithComplexProperty
    {
        public TestObject TestObject1 { get; set; }
    }

    public class SubTestObject : TestObject
    {
        public int Int2 { get; set; }
        public bool Bool2 { get; set; }
    }
}
