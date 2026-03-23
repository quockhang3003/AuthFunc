using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Common;

public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? TraceId { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static PaginatedResponse<T> Create(List<T> data, int totalCount, int pageNumber, int pageSize)
        {
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return new PaginatedResponse<T>
            {
                Data = data,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };
        }
    }

    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        [Range(1, int.MaxValue)]
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        [Range(1, 100)]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : value > 100 ? 100 : value;
        }

        public int Skip => (PageNumber - 1) * PageSize;
        public int Take => PageSize;
    }

    public class OperationResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Exception? Exception { get; set; }

        public static OperationResult<T> SuccessResult(T data)
        {
            return new OperationResult<T> { Success = true, Data = data };
        }

        public static OperationResult<T> FailureResult(string errorMessage, Exception? exception = null)
        {
            return new OperationResult<T> 
            { 
                Success = false, 
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }