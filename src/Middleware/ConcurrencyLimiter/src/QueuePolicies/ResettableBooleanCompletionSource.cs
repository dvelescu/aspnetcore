// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

/// <summary>
/// Custom awaiter to allow the StackPolicy to reduce allocations.
/// When this completion source has its result checked, it resets itself and returns itself to the cache of its parent StackPolicy.
/// Then when the StackPolicy needs a new completion source, it tries to get one from its cache, otherwise it allocates.
/// </summary>
internal class ResettableBooleanCompletionSource : IValueTaskSource<bool>
{
    ManualResetValueTaskSourceCore<bool> _valueTaskSource;
    private readonly StackPolicy _queue;

    public ResettableBooleanCompletionSource(StackPolicy queue)
    {
        _queue = queue;
        _valueTaskSource.RunContinuationsAsynchronously = true;
    }

    public ValueTask<bool> GetValueTask()
    {
        return new ValueTask<bool>(this, _valueTaskSource.Version);
    }

    bool IValueTaskSource<bool>.GetResult(short token)
    {
        var isValid = token == _valueTaskSource.Version;
        try
        {
            return _valueTaskSource.GetResult(token);
        }
        finally
        {
            if (isValid)
            {
                _valueTaskSource.Reset();
                _queue._cachedResettableTCS = this;
            }
        }
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _valueTaskSource.GetStatus(token);
    }

    void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _valueTaskSource.OnCompleted(continuation, state, token, flags);
    }

    public void Complete(bool result)
    {
        _valueTaskSource.SetResult(result);
    }
}
