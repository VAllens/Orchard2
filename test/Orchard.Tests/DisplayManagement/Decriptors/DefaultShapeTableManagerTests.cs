﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Orchard.DisplayManagement.Descriptors;
using Orchard.DisplayManagement.Implementation;
using Orchard.DisplayManagement.Shapes;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Features;
using Orchard.Environment.Extensions.Loaders;
using Orchard.Environment.Extensions.Manifests;
using Orchard.Environment.Shell;
using Orchard.Events;
using Orchard.Tests.Stubs;
using Xunit;

namespace Orchard.Tests.DisplayManagement.Decriptors
{
    public class DefaultShapeTableManagerTests
    {
        IServiceProvider _serviceProvider;

        public class StubEventBus : IEventBus
        {
            public Task NotifyAsync(string message, IDictionary<string, object> arguments)
            {
                return null;
            }

            public Task NotifyAsync<TEventHandler>(Expression<Func<TEventHandler, Task>> eventNotifier) where TEventHandler : IEventHandler
            {
                return null;
            }

            public void Subscribe(string message, Func<IServiceProvider, IDictionary<string, object>, Task> action)
            {
            }
        }

        private class TestFeatureInfo : IFeatureInfo
        {
            public string[] Dependencies { get; set; } = new string[0];
            public IExtensionInfo Extension { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public double Priority { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public bool DependencyOn(IFeatureInfo feature)
            {
                return false;
            }
        }

        private class TestModuleExtensionInfo : IExtensionInfo
        {
            public TestModuleExtensionInfo(string name)
            {
                var dic1 = new Dictionary<string, string>()
                {
                    {"name", name},
                    {"desciption", name},
                    {"type", "module"},
                };

                var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.Add(memConfigSrc1);

                Manifest = new ManifestInfo(configurationBuilder.Build(), "module");

                var features =
                    new List<IFeatureInfo>()
                    {
                        {new FeatureInfo(name, name, 0D, "", "", this, new string[0])}
                    };

                Features = new FeatureInfoList(features);
            }

            public IFileInfo ExtensionFileInfo { get; set; }
            public IFeatureInfoList Features { get; set; }
            public string Id { get; set; }
            public IManifestInfo Manifest { get; set; }
            public string SubPath { get; set; }
            public bool Exists => true;
        }

        private class TestThemeExtensionInfo : IExtensionInfo
        {
            public TestThemeExtensionInfo(string name)
            {
                var dic1 = new Dictionary<string, string>()
                {
                    {"name", name},
                    {"desciption", name},
                    {"type", "theme"},
                };

                var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.Add(memConfigSrc1);

                Manifest = new ManifestInfo(configurationBuilder.Build(), "theme");

                var features =
                    new List<IFeatureInfo>()
                    {
                        {new FeatureInfo(name, name, 0D, "", "", this, new string[0])}
                    };

                Features = new FeatureInfoList(features);

                Id = name;
            }

            public TestThemeExtensionInfo(string name, IFeatureInfo baseTheme)
            {
                var dic1 = new Dictionary<string, string>()
                {
                    {"name", name},
                    {"desciption", name},
                    {"type", "theme"},
                    {"basetheme", baseTheme.Id}
                };

                var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.Add(memConfigSrc1);

                Manifest = new ManifestInfo(configurationBuilder.Build(), "theme");

                var features =
                    new List<IFeatureInfo>()
                    {
                        {new FeatureInfo(name, name, 0D, "", "", this, new string[] { baseTheme.Id })}
                    };

                Features = new FeatureInfoList(features);

                Id = name;
            }

            public IFileInfo ExtensionFileInfo { get; set; }
            public IFeatureInfoList Features { get; set; }
            public string Id { get; set; }
            public IManifestInfo Manifest { get; set; }
            public string SubPath { get; set; }
            public bool Exists => true;
        }

        public DefaultShapeTableManagerTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging();
            serviceCollection.AddMemoryCache();
            serviceCollection.AddScoped<IFeatureManager, StubFeatureManager>();
            serviceCollection.AddScoped<IShellFeaturesManager, TestShellFeaturesManager>();
            serviceCollection.AddScoped<IShapeTableManager, DefaultShapeTableManager>();
            serviceCollection.AddScoped<IEventBus, StubEventBus>();
            serviceCollection.AddSingleton<ITypeFeatureProvider, TypeFeatureProvider>();

            var testFeatureExtensionInfo = new TestModuleExtensionInfo("Testing");
            var theme1FeatureExtensionInfo = new TestThemeExtensionInfo("Theme1");
            var baseThemeFeatureExtensionInfo = new TestThemeExtensionInfo("BaseTheme");
            var derivedThemeFeatureExtensionInfo = new TestThemeExtensionInfo("DerivedTheme", baseThemeFeatureExtensionInfo.Features.First());

            var features = new[] {
                testFeatureExtensionInfo.Features.First(),
                theme1FeatureExtensionInfo.Features.First(),
                derivedThemeFeatureExtensionInfo.Features.First(),
                baseThemeFeatureExtensionInfo.Features.First()
            };

            serviceCollection.AddSingleton<IExtensionManager>(new TestExtensionManager(features));

            TestShapeProvider.FeatureShapes = new Dictionary<IFeatureInfo, IEnumerable<string>> {
                { TestFeature(), new [] {"Hello"} },
                { features[1], new [] {"Theme1Shape"} },
                { features[2], new [] {"DerivedShape", "OverriddenShape"} },
                { features[3], new [] {"BaseShape", "OverriddenShape"} }
            };

            serviceCollection.AddScoped<IShapeTableProvider, TestShapeProvider>();
            serviceCollection.AddScoped<TestShapeProvider>((x => (TestShapeProvider)x.GetService<IShapeTableProvider>()));

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        static IFeatureInfo TestFeature()
        {
            return new TestFeatureInfo
            {
                Id = "Testing",
                Dependencies = new string[0],
                Extension = new TestModuleExtensionInfo("Testing")
            };
        }

        public class TestShellFeaturesManager : IShellFeaturesManager
        {
            private readonly IExtensionManager _extensionManager;

            public TestShellFeaturesManager(IExtensionManager extensionManager)
            {
                _extensionManager = extensionManager;
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.GetEnabledFeaturesAsync()
            {
                var extensions = _extensionManager.GetExtensions();
                var features = extensions.Features;

                return Task.FromResult(features.AsEnumerable());
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.EnableFeaturesAsync(IEnumerable<IFeatureInfo> features)
            {
                throw new NotImplementedException();
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.EnableFeaturesAsync(IEnumerable<IFeatureInfo> features, bool force)
            {
                throw new NotImplementedException();
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.GetDisabledFeaturesAsync()
            {
                throw new NotImplementedException();
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.DisableFeaturesAsync(IEnumerable<IFeatureInfo> features)
            {
                throw new NotImplementedException();
            }

            Task<IEnumerable<IFeatureInfo>> IShellFeaturesManager.DisableFeaturesAsync(IEnumerable<IFeatureInfo> features, bool force)
            {
                throw new NotImplementedException();
            }
        }

        public class TestExtensionManager : IExtensionManager
        {
            private IEnumerable<IFeatureInfo> _features;
            public TestExtensionManager(IEnumerable<IFeatureInfo> features)
            {
                _features = features;
            }

            public IFeatureInfoList GetFeatureDependencies(string featureId)
            {
                throw new NotImplementedException();
            }

            public IExtensionInfo GetExtension(string extensionId)
            {
                return _features.Select(x => x.Extension).First(x => x.Id == extensionId);
            }

            public IExtensionInfoList GetExtensions()
            {
                return new ExtensionInfoList(_features.Select(x => x.Extension).ToList());
            }

            public IFeatureInfoList GetFeatures(string[] featureIds)
            {
                throw new NotImplementedException();
            }

            public Task<ExtensionEntry> LoadExtensionAsync(IExtensionInfo extensionInfo)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<ExtensionEntry>> LoadExtensionsAsync(IEnumerable<IExtensionInfo> extensionInfos)
            {
                throw new NotImplementedException();
            }

            public Task<FeatureEntry> LoadFeatureAsync(IFeatureInfo feature)
            {
                return Task.FromResult((FeatureEntry)new NonCompiledFeatureEntry(feature));
            }

            public Task<IEnumerable<FeatureEntry>> LoadFeaturesAsync(IEnumerable<IFeatureInfo> features)
            {
                return Task.FromResult(features.Select(x => new NonCompiledFeatureEntry(x)).AsEnumerable<FeatureEntry>());
            }

            public IFeatureInfoList GetDependentFeatures(string featureId, IFeatureInfo[] featuresToSearch)
            {
                throw new NotImplementedException();
            }
        }

        public class TestShapeProvider : IShapeTableProvider
        {
            public static IDictionary<IFeatureInfo, IEnumerable<string>> FeatureShapes;

            public Action<ShapeTableBuilder> Discover = x => { };

            void IShapeTableProvider.Discover(ShapeTableBuilder builder)
            {
                foreach (var pair in FeatureShapes)
                {
                    foreach (var shape in pair.Value)
                    {
                        builder.Describe(shape).From(pair.Key).BoundAs(pair.Key.Id, null);
                    }
                }

                Discover(builder);
            }
        }

        [Fact]
        public void ManagerCanBeResolved()
        {
            var manager = _serviceProvider.GetService<IShapeTableManager>();
            Assert.NotNull(manager);
        }

        [Fact]
        public void DefaultShapeTableIsReturnedForNullOrEmpty()
        {
            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var shapeTable1 = manager.GetShapeTable(null);
            var shapeTable2 = manager.GetShapeTable(string.Empty);
            Assert.NotNull(shapeTable1.Descriptors["Hello"]);
            Assert.NotNull(shapeTable2.Descriptors["Hello"]);
        }

        [Fact]
        public void CallbackAlterationsContributeToDescriptor()
        {
            Action<ShapeCreatingContext> cb1 = x => { };
            Action<ShapeCreatedContext> cb2 = x => { };
            Action<ShapeDisplayingContext> cb3 = x => { };
            Action<ShapeDisplayedContext> cb4 = x => { };

            _serviceProvider.GetService<TestShapeProvider>().Discover =
                builder => builder.Describe("Foo").From(TestFeature())
                               .OnCreating(cb1)
                               .OnCreated(cb2)
                               .OnDisplaying(cb3)
                               .OnDisplayed(cb4);

            var manager = _serviceProvider.GetService<IShapeTableManager>();

            var foo = manager.GetShapeTable(null).Descriptors["Foo"];

            Assert.Same(cb1, foo.Creating.Single());
            Assert.Same(cb2, foo.Created.Single());
            Assert.Same(cb3, foo.Displaying.Single());
            Assert.Same(cb4, foo.Displayed.Single());
        }
        [Fact]
        public void DefaultPlacementIsReturnedByDefault()
        {
            var manager = _serviceProvider.GetService<IShapeTableManager>();

            var hello = manager.GetShapeTable(null).Descriptors["Hello"];
            hello.DefaultPlacement = "Header:5";
            var result = hello.Placement(null);
            Assert.Equal("Header:5", result.Location);
        }

        [Fact]
        public void DescribedPlacementIsReturnedIfNotNull()
        {
            var shapeDetail = new Shape { Metadata = new ShapeMetadata { DisplayType = "Detail" } };
            var shapeSummary = new Shape { Metadata = new ShapeMetadata { DisplayType = "Summary" } };
            var shapeTile = new Shape { Metadata = new ShapeMetadata { DisplayType = "Tile" } };

            _serviceProvider.GetService<TestShapeProvider>().Discover =
                builder => builder.Describe("Hello1").From(TestFeature())
                    .Placement(ctx => ctx.Shape.Metadata.DisplayType == "Detail" ? new PlacementInfo { Location = "Main" } : null)
                    .Placement(ctx => ctx.Shape.Metadata.DisplayType == "Summary" ? new PlacementInfo { Location = "" } : null);

            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var hello = manager.GetShapeTable(null).Descriptors["Hello1"];
            var result1 = hello.Placement(new ShapePlacementContext { Shape = shapeDetail });
            var result2 = hello.Placement(new ShapePlacementContext { Shape = shapeSummary });
            var result3 = hello.Placement(new ShapePlacementContext { Shape = shapeTile });
            hello.DefaultPlacement = "Header:5";
            var result4 = hello.Placement(new ShapePlacementContext { Shape = shapeDetail });
            var result5 = hello.Placement(new ShapePlacementContext { Shape = shapeSummary });
            var result6 = hello.Placement(new ShapePlacementContext { Shape = shapeTile });

            Assert.Equal("Main", result1.Location);
            Assert.Empty(result2.Location);
            Assert.Null(result3);
            Assert.Equal("Main", result4.Location);
            Assert.Empty(result5.Location);
            Assert.Equal("Header:5", result6.Location);
        }

        [Fact]
        public void TwoArgumentVariationDoesSameThing()
        {
            var shapeDetail = new Shape { Metadata = new ShapeMetadata { DisplayType = "Detail" } };
            var shapeSummary = new Shape { Metadata = new ShapeMetadata { DisplayType = "Summary" } };
            var shapeTile = new Shape { Metadata = new ShapeMetadata { DisplayType = "Tile" } };

            _serviceProvider.GetService<TestShapeProvider>().Discover =
                builder => builder.Describe("Hello2").From(TestFeature())
                    .Placement(ctx => ctx.Shape.Metadata.DisplayType == "Detail", new PlacementInfo { Location = "Main" })
                    .Placement(ctx => ctx.Shape.Metadata.DisplayType == "Summary", new PlacementInfo { Location = "" });

            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var hello = manager.GetShapeTable(null).Descriptors["Hello2"];
            var result1 = hello.Placement(new ShapePlacementContext { Shape = shapeDetail });
            var result2 = hello.Placement(new ShapePlacementContext { Shape = shapeSummary });
            var result3 = hello.Placement(new ShapePlacementContext { Shape = shapeTile });
            hello.DefaultPlacement = "Header:5";
            var result4 = hello.Placement(new ShapePlacementContext { Shape = shapeDetail });
            var result5 = hello.Placement(new ShapePlacementContext { Shape = shapeSummary });
            var result6 = hello.Placement(new ShapePlacementContext { Shape = shapeTile });

            Assert.Equal("Main", result1.Location);
            Assert.Empty(result2.Location);
            Assert.Null(result3);
            Assert.Equal("Main", result4.Location);
            Assert.Empty(result5.Location);
            Assert.Equal("Header:5", result6.Location);
        }

        [Fact(Skip = "Path not supported yet")]
        public void PathConstraintShouldMatch()
        {
            // all path have a trailing / as per the current implementation
            // todo: (sebros) find a way to 'use' the current implementation in DefaultContentDisplay.BindPlacement instead of emulating it

            var rules = new[] {
                Tuple.Create("~/my-blog", "~/my-blog/", true),
                Tuple.Create("~/my-blog/", "~/my-blog/", true),

                // star match
                Tuple.Create("~/my-blog*", "~/my-blog/", true),
                Tuple.Create("~/my-blog*", "~/my-blog/my-post/", true),
                Tuple.Create("~/my-blog/*", "~/my-blog/", true),
                Tuple.Create("~/my-blog/*", "~/my-blog123/", false),
                Tuple.Create("~/my-blog*", "~/my-blog123/", true)
            };

            foreach (var rule in rules)
            {
                var path = rule.Item1;
                var context = rule.Item2;
                var match = rule.Item3;

                _serviceProvider.GetService<TestShapeProvider>().Discover =
                    builder => builder.Describe("Hello").From(TestFeature())
                                   .Placement(ctx => true, new PlacementInfo { Location = "Match" });

                var manager = _serviceProvider.GetService<IShapeTableManager>();
                var hello = manager.GetShapeTable(null).Descriptors["Hello"];
                //var result = hello.Placement(new ShapePlacementContext { Path = context });

                //if (match)
                //{
                //    Assert.Equal("Match", result.Location);
                //}
                //else
                //{
                //    Assert.Null(result.Location);
                //}
            }
        }

        [Fact]
        public void OnlyShapesFromTheGivenThemeAreProvided()
        {
            _serviceProvider.GetService<TestShapeProvider>();
            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var table = manager.GetShapeTable("Theme1");
            Assert.True(table.Descriptors.ContainsKey("Theme1Shape"));
            Assert.False(table.Descriptors.ContainsKey("DerivedShape"));
            Assert.False(table.Descriptors.ContainsKey("BaseShape"));
        }

        [Fact]
        public void ShapesFromTheBaseThemeAreProvided()
        {
            _serviceProvider.GetService<TestShapeProvider>();
            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var table = manager.GetShapeTable("DerivedTheme");
            Assert.False(table.Descriptors.ContainsKey("Theme1Shape"));
            Assert.True(table.Descriptors.ContainsKey("DerivedShape"));
            Assert.True(table.Descriptors.ContainsKey("BaseShape"));
        }

        [Fact]
        public void DerivedThemesCanOverrideBaseThemeShapeBindings()
        {
            _serviceProvider.GetService<TestShapeProvider>();
            var manager = _serviceProvider.GetService<IShapeTableManager>();
            var table = manager.GetShapeTable("DerivedTheme");
            Assert.True(table.Bindings.ContainsKey("OverriddenShape"));
            Assert.StrictEqual("DerivedTheme", table.Descriptors["OverriddenShape"].BindingSource);
        }
    }
}