using System;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DDD
{
    /// <summary>
    /// Wrapper around a result of a domain action.
    /// </summary>
    public class Result
    {
        #region Properties

        /// <summary>
        /// Flag that indicates if result is success or failure.
        /// </summary>
        public bool IsSuccess { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new Result with a specific success flag.
        /// </summary>
        /// <param name="isSuccess">Succes flag value.</param>
        protected Result(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Combine more results with the current one.
        /// </summary>
        /// <param name="results">Other results to combine to.</param>
        /// <returns>A result Ok if all are ok, or a failed result if one is failed</returns>
        public Result Combine(params Result[] results)
        {
            if (results == null)
            {
                return this;
            }
            foreach (var item in results)
            {
                if (!item.IsSuccess)
                {
                    return Fail();
                }
            }
            return Ok();
        }

        /// <summary>
        /// Defines a continuation function to execute when result is successful.
        /// Action will not be invoked if result is failed.
        /// </summary>
        /// <param name="successContinuation">Continuation action.</param>
        public Result OnSuccess(Action successContinuation)
            => LambdaInvokation(IsSuccess, successContinuation);

        /// <summary>
        /// Defines an asynchronous continuation function to execute when result is successful.
        /// Action will not be invoked if result is ok.
        /// </summary>
        /// <param name="asyncSuccessContinuation">Asynchronous continuation action.</param>
        public Task<Result> OnSuccessAsync(Func<Task> asyncSuccessContinuation)
            => AsyncLambdaInvokationAsync(IsSuccess, asyncSuccessContinuation);

        /// <summary>
        /// Defines a continuation function to execute when result is success.
        /// Action will not be invoked if result is failed.
        /// </summary>
        /// <param name="failedContinuation">Continuation action.</param>
        public Result OnFailure(Action failedContinuation)
            => LambdaInvokation(!IsSuccess, failedContinuation);

        /// <summary>
        /// Defines an asynchronous continuation function to execute when result is success.
        /// Action will not be invoked if result is ok.
        /// </summary>
        /// <param name="asyncFailedContinuation">Asynchronous continuation action.</param>
        public Task<Result> OnFailureAsync(Func<Task> asyncFailedContinuation)
            => AsyncLambdaInvokationAsync(!IsSuccess, asyncFailedContinuation);

        #endregion

        #region Public static methods

        /// <summary>
        /// Get a standard failure result that doesn't holds any
        /// specific value.
        /// </summary>
        /// <returns>Failure result.</returns>
        public static Result Fail() => new Result(false);
        /// <summary>
        /// Get a standard success result.
        /// </summary>
        /// <returns>Success result.</returns>
        public static Result Ok() => new Result(true);
        /// <summary>
        /// Get a success result with a defined value.
        /// </summary>
        /// <typeparam name="T">Type of value to use in result.</typeparam>
        /// <param name="value">Value to use in result.</param>
        /// <returns>Succes result with value.</returns>
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
        /// <summary>
        /// Get a failed result with a defined value.
        /// </summary>
        /// <typeparam name="T">Type of value to use in result.</typeparam>
        /// <param name="value">Value to use in result.</param>
        /// <returns>Failed result with value.</returns>
        public static Result<T> Fail<T>(T value) => Result<T>.Fail(value);

        #endregion

        #region Operator

        public static implicit operator bool(Result r)
            => r.IsSuccess;

        public static implicit operator Task<Result>(Result r)
            => Task.FromResult(r);

        #endregion

        #region Private methods

        private Result LambdaInvokation(bool shouldInvoke, Action lambda)
        {
            if (shouldInvoke)
            {
                lambda.Invoke();
            }
            return this;
        }

        private async Task<Result> AsyncLambdaInvokationAsync(bool shouldInvoke, Func<Task> lambda)
        {
            if (shouldInvoke)
            {
                await lambda.Invoke().ConfigureAwait(false);
            }
            return this;
        }

        #endregion

    }
}
