﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using EnvDTE;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.VsEvents
{
    public static class ProjectRetargetingUtilityTest
    {
        [Fact]
        public static void GetPackagesToBeReinstalledWhenProjectKindIsNull()
        {
            // Arrange
            Mock<Project> mockProject = new Mock<Project>();
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A"));

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(mockProject.Object, localRepository);

            // Assert
            Assert.True(packagesToBeReinstalled.IsEmpty());
        }

        [Fact]
        public static void GetPackagesToBeReinstalledWhenProjectIsNotOfSupportedType()
        {
            // Arrange
            Mock<Project> mockProject = new Mock<Project>();
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A"));

            mockProject.Setup(p => p.Kind).Returns(Guid.NewGuid().ToString());

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(mockProject.Object, localRepository);

            // Assert
            Assert.True(packagesToBeReinstalled.IsEmpty());
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsEmptyListWhenNuGetIsNotInUseInAProject()
        {
            // Arrange
            Mock<Project> mockProject = new Mock<Project>();
            MockPackageRepository localRepository = new MockPackageRepository();

            // Setup project kind to a supported value. This makes sure that the check for existence of packages.config happens
            mockProject.Setup(p => p.Kind).Returns(VsConstants.CsharpProjectTypeGuid);

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(mockProject.Object, localRepository);

            // Assert
            Assert.True(packagesToBeReinstalled.IsEmpty());
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsPackageTargetingSingleFramework()
        {
            // Create a packageA which has as assembly reference only in net40. Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net40\foo.dll" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(1, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "A");
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsPackageTargetingMultipleFrameworks()
        {
            // Create a packageA which has as assembly reference in net 30 and net40. Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net30\bar.dll", @"lib\net40\foo.dll" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(1, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "A");
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsEmptyListForPackageTargetingSingleFramework()
        {
            // Create a packageA which has as assembly reference only in net30.  Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD NOT REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net30\foo.dll" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.True(packagesToBeReinstalled.IsEmpty());
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsEmptyListForPackageTargetingMultipleFrameworks()
        {
            // Create a packageA which has as assembly reference in net 20 and net30. Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net20\bar.dll", @"lib\net30\foo.dll" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.True(packagesToBeReinstalled.IsEmpty());
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsPackageTargetingSingleFrameworkBasedOnContent()
        {
            // Create a packageA which has as content only in net40. Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", content: new[] { @"net40\bar.txt" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(1, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "A");
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsPackageTargetingSingleFrameworkBasedOnTools()
        {
            // Create a packageA which has as content only in net40. Create a package reference corresponding to this package with the project target framework as 'net40'
            // Now, Try and check if the created packagereference on a project with targetframework of net35 will require reinstallation. IT SHOULD REQUIRE REINSTALLATION

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, null, tools: new[] { @"net40\init.ps1" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(1, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "A");
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsMultiplePackagesToBeReinstalled()
        {
            // Create packageA, packageB and packageC such that only packageB and packageC will need to be reinstalled for changing
            // targetframework for project from net40 to net35. And, validate that GetPackagesToBeReinstalled returns the correct list

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net20\foo.dll" });
            IPackage packageB = PackageUtility.CreatePackage("B", "1.0.0", content: new[] { @"net40\foo.txt" });
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0.0", null, null, tools: new[] { @"net40\bar.ps1" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));
            packageReferences.Add(new PackageReference("B", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));
            packageReferences.Add(new PackageReference("C", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.0"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(2, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "B");
            Assert.Equal(packagesToBeReinstalled[1].Id, "C");
        }

        [Fact]
        public static void GetPackagesToBeReinstalledReturnsPackagesInstalledAgainstDifferentProjectFrameworks()
        {
            // Create packageA and packageB
            // packageA has files targeting net35 and was installed against net30
            // packageB has files targeting net40 and was installed against net45
            // current targetFramework of the project they are installed in is net35
            // They should BOTH be reinstalled

            // Arrange
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0.0", null, assemblyReferences: new[] { @"lib\net35\foo.dll" });
            IPackage packageB = PackageUtility.CreatePackage("B", "1.0.0", content: new[] { @"net40\foo.txt" });

            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            List<PackageReference> packageReferences = new List<PackageReference>();
            packageReferences.Add(new PackageReference("A", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=3.0"), isDevelopmentDependency: false));
            packageReferences.Add(new PackageReference("B", new SemanticVersion("1.0.0"), null, new FrameworkName(".NETFramework, Version=4.5"), isDevelopmentDependency: false));

            FrameworkName projectFramework = new FrameworkName(".NETFramework, Version=3.5");

            // Act
            var packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(projectFramework, packageReferences, localRepository);

            // Assert
            Assert.Equal(2, packagesToBeReinstalled.Count);
            Assert.Equal(packagesToBeReinstalled[0].Id, "A");
            Assert.Equal(packagesToBeReinstalled[1].Id, "B");
        }
    }
}