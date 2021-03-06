﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using SimuVirus.deploy;

namespace SimuVirus.tools
{
    class PEreader
    {
        public api.IMAGE_DOS_HEADER dosHeader;
        public api.IMAGE_FILE_HEADER fileHeader;
        public api.IMAGE_OPTIONAL_HEADER32 optionalHeader32;
        public api.IMAGE_OPTIONAL_HEADER64 optionalHeader64;
        public api.IMAGE_SECTION_HEADER[] imageSectionHeaders;
        public uint addressOfEntryPoint;
        public ulong imageBase;
        private FileStream stream;
        private string filePath;

        public static T FromBinaryReader<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public void initialize(string path)
        {
            //readPeFile(path);
            filePath = path;
            run(path);
        }

        public bool Is32BitHeader
        {
            get
            {
                UInt16 IMAGE_FILE_32BIT_MACHINE = 0x0100;
                return (IMAGE_FILE_32BIT_MACHINE & fileHeader.Characteristics) == IMAGE_FILE_32BIT_MACHINE;
            }
        }

        public FileStream launchNewFile()
        {
            FileStream newFile = File.Create(Info.currentDirectory + "\\infected.exe");
            return newFile;
        }

        public void close(FileStream file)
        {
            file.Close();
        }

        public void copyStreamFromSrc(FileStream dest)
        {
            stream = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            stream.CopyTo(dest);
            stream.Close();
        }

        public void modifyStream(FileStream file, int addr, byte[] data)
        {
            file.Position = addr;
            if (file.CanWrite)
                file.Write(data, 0, data.Length);
        }

        private void run(string filePath)
        {
            using (stream = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(stream);
                dosHeader = FromBinaryReader<api.IMAGE_DOS_HEADER>(reader);

                stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

                UInt32 ntHeadersSignature = reader.ReadUInt32();
                fileHeader = FromBinaryReader<api.IMAGE_FILE_HEADER>(reader);
                if (this.Is32BitHeader)
                {
                    optionalHeader32 = FromBinaryReader<api.IMAGE_OPTIONAL_HEADER32>(reader);
                    addressOfEntryPoint = optionalHeader32.AddressOfEntryPoint;
                    imageBase = optionalHeader32.ImageBase;
                }
                else
                {
                    optionalHeader64 = FromBinaryReader<api.IMAGE_OPTIONAL_HEADER64>(reader);
                    addressOfEntryPoint = optionalHeader64.AddressOfEntryPoint;
                    imageBase = optionalHeader64.ImageBase;
                }

                imageSectionHeaders = new api.IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];
                for (int headerNo = 0; headerNo < imageSectionHeaders.Length; ++headerNo)
                {
                    imageSectionHeaders[headerNo] = FromBinaryReader<api.IMAGE_SECTION_HEADER>(reader);
                }
            }
        }
    }
}
