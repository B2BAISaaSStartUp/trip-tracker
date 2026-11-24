namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Search, sort and paging options for the owner's trip list (FR-08).</summary>
public sealed record ListTripsQuery
{
    /// <summary>Page size used when none is given.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page size accepted.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Case-insensitive title filter; empty means no filter.</summary>
    public string? Search { get; init; }

    /// <summary>Sort by start date, newest first unless set to <c>false</c>.</summary>
    public bool NewestFirst { get; init; } = true;

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Items per page, 1 to <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
