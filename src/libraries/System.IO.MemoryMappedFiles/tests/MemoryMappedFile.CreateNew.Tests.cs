// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Tests for MemoryMappedFile.CreateNew.
    /// </summary>
    public class MemoryMappedFileTests_CreateNew : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Tests invalid arguments to the CreateNew mapName parameter.
        /// </summary>
        [Fact]
        public void InvalidArguments_MapName()
        {
            // Empty string is an invalid map name
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateNew(string.Empty, 4096));
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateNew(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<ArgumentException>(() => MemoryMappedFile.CreateNew(string.Empty, 4096, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateNew capacity parameter.
        /// </summary>
        [Theory]
        [InlineData(0)] // default size is invalid with CreateNew as there's nothing to expand to match
        [InlineData(-100)] // negative values don't make sense
        public void InvalidArguments_Capacity(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateNew(null, capacity));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateNew(null, capacity, MemoryMappedFileAccess.ReadWrite));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateNew(null, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateNew access parameter.
        /// </summary>
        [Theory]
        [InlineData((MemoryMappedFileAccess)42)]
        [InlineData((MemoryMappedFileAccess)(-2))]
        public void InvalidArguments_Access(MemoryMappedFileAccess access)
        {
            // Out of range values
            Assert.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateNew(null, 4096, access));
            Assert.Throws<ArgumentOutOfRangeException>("access", () => MemoryMappedFile.CreateNew(null, 4096, access, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateNew access parameter, specifically MemoryMappedFileAccess.Write.
        /// </summary>
        [Fact]
        public void InvalidArguments_WriteAccess()
        {
            // Write-only access isn't allowed, as it'd be useless
            Assert.Throws<ArgumentException>("access", () => MemoryMappedFile.CreateNew(null, 4096, MemoryMappedFileAccess.Write));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateNew options parameter.
        /// </summary>
        [Theory]
        [InlineData((MemoryMappedFileOptions)42)]
        [InlineData((MemoryMappedFileOptions)(-2))]
        public void InvalidArguments_Options(MemoryMappedFileOptions options)
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () => MemoryMappedFile.CreateNew(null, 4096, MemoryMappedFileAccess.Read, options, HandleInheritability.None));
        }

        /// <summary>
        /// Tests invalid arguments to the CreateNew inheritability parameter.
        /// </summary>
        [Theory]
        [InlineData((HandleInheritability)42)]
        [InlineData((HandleInheritability)(-2))]
        public void InvalidArguments_Inheritability(HandleInheritability inheritability)
        {
            Assert.Throws<ArgumentOutOfRangeException>("inheritability", () => MemoryMappedFile.CreateNew(null, 4096, MemoryMappedFileAccess.Read, MemoryMappedFileOptions.None, inheritability));
        }

        /// <summary>
        /// Test the exceptional behavior when attempting to create a map so large it's not supported.
        /// </summary>
        [Fact, PlatformSpecific(PlatformID.Windows)]
        public void TooLargeCapacity_Windows()
        {
            if (IntPtr.Size == 4)
            {
                Assert.Throws<ArgumentOutOfRangeException>("capacity", () => MemoryMappedFile.CreateNew(null, 1 + (long)uint.MaxValue));
            }
            else
            {
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateNew(null, long.MaxValue));
            }
        }

        /// <summary>
        /// Test the exceptional behavior when attempting to create a map so large it's not supported.
        /// </summary>
        [Fact, PlatformSpecific(PlatformID.Linux)] // Because of the file-based backing, OS X pops up a warning dialog about being out-of-space (even though we clean up immediately)
        public void TooLargeCapacity_Unix()
        {
            // On Windows we fail with too large a capacity as part of the CreateNew call.
            // On Unix that exception may happen a bit later, as part of creating the view,
            // due to differences in OS behaviors and Unix not actually having a notion of
            // a view separate from a map.  It could also come from CreateNew, depending
            // on what backing store is being used.
            Assert.Throws<IOException>(() =>
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, long.MaxValue))
                {
                    mmf.CreateViewAccessor().Dispose();
                }
            });
        }

        /// <summary>
        /// Test to verify that map names are left unsupported on Unix.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.AnyUnix)]
        [MemberData("CreateValidMapNames")]
        public void MapNamesNotSupported_Unix(string mapName)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateNew(mapName, 4096));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateNew(mapName, 4096, MemoryMappedFileAccess.Read));
            Assert.Throws<PlatformNotSupportedException>(() => MemoryMappedFile.CreateNew(mapName, 4096, MemoryMappedFileAccess.Read, MemoryMappedFileOptions.None, HandleInheritability.None));
        }

        /// <summary>
        /// Test to verify a variety of map names work correctly on Windows.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.Windows)]
        [MemberData("CreateValidMapNames")]
        [InlineData(null)]
        public void ValidMapNames_Windows(string name)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096))
            {
                ValidateMemoryMappedFile(mmf, 4096);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096, MemoryMappedFileAccess.Read))
            {
                ValidateMemoryMappedFile(mmf, 4096, MemoryMappedFileAccess.Read);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096, MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.Inheritable))
            {
                ValidateMemoryMappedFile(mmf, 4096, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable);
            }
        }

        /// <summary>
        /// Test to verify map names are handled appropriately, causing a conflict when they're active but
        /// reusable in a sequential manner.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.Windows)]
        [MemberData("CreateValidMapNames")]
        public void ReusingNames_Windows(string name)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096))
            {
                ValidateMemoryMappedFile(mmf, 4096);
                Assert.Throws<IOException>(() => MemoryMappedFile.CreateNew(name, 4096));
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096))
            {
                ValidateMemoryMappedFile(mmf, 4096, MemoryMappedFileAccess.ReadWrite);
            }
        }

        /// <summary>
        /// Test various combinations of arguments to CreateNew, validating the created maps each time they're created.
        /// </summary>
        [Theory]
        [MemberData("MemberData_ValidArgumentCombinations",
            new string[] { null, "CreateUniqueMapName()" },
            new long[] { 1, 256, -1 /*pagesize*/, 10000 },
            new MemoryMappedFileAccess[] { MemoryMappedFileAccess.Read, MemoryMappedFileAccess.ReadExecute, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileAccess.CopyOnWrite },
            new MemoryMappedFileOptions[] { MemoryMappedFileOptions.None, MemoryMappedFileOptions.DelayAllocatePages },
            new HandleInheritability[] { HandleInheritability.None, HandleInheritability.Inheritable })]
        public void ValidArgumentCombinations(
            string mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity))
            {
                ValidateMemoryMappedFile(mmf, capacity);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access))
            {
                ValidateMemoryMappedFile(mmf, capacity, access);
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(mapName, capacity, access, options, inheritability))
            {
                ValidateMemoryMappedFile(mmf, capacity, access, inheritability);
            }
        }

        /// <summary>
        /// Provides input data to the ValidArgumentCombinations tests, yielding the full matrix
        /// of combinations of input values provided, except for those that are known to be unsupported
        /// (e.g. non-null map names on Unix), and with appropriate values substituted in for placeholders
        /// listed in the MemberData attribute (e.g. actual system page size instead of -1).
        /// </summary>
        /// <param name="mapNames">
        /// The names to yield.  
        /// non-null may be excluded based on platform.
        /// "CreateUniqueMapName()" will be translated to an invocation of that method.
        /// </param>
        /// <param name="capacities">The capacities to yield.  -1 will be translated to system page size.</param>
        /// <param name="accesses">The accesses to yield</param>
        /// <param name="options">The options to yield.</param>
        /// <param name="inheritabilities">The inheritabilities to yield.</param>
        public static IEnumerable<object[]> MemberData_ValidArgumentCombinations(
            object[] mapNames, object[] capacities, object[] accesses, object[] options, object[] inheritabilities)
        {
            foreach (string tmpMapName in mapNames)
            {
                if (tmpMapName != null && !MapNamesSupported)
                {
                    continue;
                }
                string mapName = tmpMapName == "CreateUniqueMapName()" ? 
                    CreateUniqueMapName() : 
                    tmpMapName;

                foreach (long tmpCapacity in capacities)
                {
                    long capacity = tmpCapacity == -1 ? 
                        s_pageSize.Value : 
                        tmpCapacity;

                    foreach (MemoryMappedFileAccess access in accesses)
                    {
                        foreach (MemoryMappedFileOptions option in options)
                        {
                            foreach (HandleInheritability inheritability in inheritabilities)
                            {
                                yield return new object[] { mapName, capacity, access, option, inheritability };
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test to verify that two unrelated maps don't share data.
        /// </summary>
        [Theory, PlatformSpecific(PlatformID.Windows)]
        [MemberData("CreateValidMapNames")]
        [InlineData(null)]
        public void DataNotPersistedBetweenMaps_Windows(string name)
        {
            // Write some data to a map newly created with the specified name
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096))
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
            {
                accessor.Write(0, 42);
            }

            // After it's closed, open a map with the same name again and verify the data's gone
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(name, 4096))
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
            {
                // Data written to previous map should not be here
                Assert.Equal(0, accessor.ReadByte(0));
            }
        }

        /// <summary>
        /// Test to verify that two unrelated maps don't share data.
        /// </summary>
        [Fact]
        public void DataNotPersistedBetweenMaps_Unix()
        {
            // same as the Windows test, but for Unix we only validate null, as other names aren't allowed
            DataNotPersistedBetweenMaps_Windows(null); 
        }

        /// <summary>
        /// Test to verify that we can have many maps open at the same time.
        /// </summary>
        [Fact]
        public void ManyConcurrentMaps()
        {
            const int NumMaps = 100, Capacity = 4096;
            var mmfs = new List<MemoryMappedFile>(Enumerable.Range(0, NumMaps).Select(_ => MemoryMappedFile.CreateNew(null, Capacity)));
            try
            {
                mmfs.ForEach(mmf => ValidateMemoryMappedFile(mmf, Capacity));
            }
            finally
            {
                mmfs.ForEach(mmf => mmf.Dispose());
            }
        }

        /// <summary>
        /// Test to verify expected capacity with regards to page size and automatically rounding up to the nearest.
        /// </summary>
        [Fact]
        public void RoundedUpCapacity()
        {
            // On both Windows and Unix, capacity is rounded up to the nearest page size.   However, 
            // the amount of capacity actually usable by the developer is supposed to be limited
            // to that specified.  That's not currently the case with the MMF APIs on Windows;
            // it is the case on Unix.
            int specifiedCapacity = 1;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, specifiedCapacity))
            using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
            {
                Assert.Equal(
                    Interop.IsWindows ? s_pageSize.Value : specifiedCapacity,
                    acc.Capacity);
            }
        }

        /// <summary>
        /// Test to verify we can dispose of a map multiple times.
        /// </summary>
        [Fact]
        public void DoubleDispose()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 4096);
            mmf.Dispose();
            mmf.Dispose();
        }

        /// <summary>
        /// Test to verify we can't create new views after the map has been disposed.
        /// </summary>
        [Fact]
        public void UnusableAfterDispose()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 4096);
            SafeMemoryMappedFileHandle handle = mmf.SafeMemoryMappedFileHandle;

            Assert.False(handle.IsClosed);
            mmf.Dispose();
            Assert.True(handle.IsClosed);

            Assert.Throws<ObjectDisposedException>(() => mmf.CreateViewAccessor());
            Assert.Throws<ObjectDisposedException>(() => mmf.CreateViewStream());
        }

    }
}
