using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Common;
public interface IApplicationDbContext
{
    DbContext DbContext { get; } // only for get
    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}
