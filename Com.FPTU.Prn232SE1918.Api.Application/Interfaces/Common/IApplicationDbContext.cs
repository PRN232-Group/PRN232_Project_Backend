using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.FPTU.Prn232SE1918.Api.Application.Interfaces.Common;
public interface IApplicationDbContext
{
    DbContext DbContext { get; } // only for get
}
