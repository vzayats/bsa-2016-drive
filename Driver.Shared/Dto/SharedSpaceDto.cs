﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Driver.Shared.Dto
{
    public class SharedSpaceDto
    {
        public IEnumerable<FileUnitDto> Files { get; set; }
        public IEnumerable<FolderUnitDto> Folders { get; set; }
    }
}
