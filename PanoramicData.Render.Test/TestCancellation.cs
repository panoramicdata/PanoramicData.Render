namespace PanoramicData.Render.Test;

using Xunit;

internal static class TestCancellation
{
	private static readonly TimeSpan DefaultRenderTimeout = TimeSpan.FromSeconds(20);

	public static CancellationTokenSource CreateRenderTimeoutTokenSource(TimeSpan? timeout = null)
	{
		var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		linked.CancelAfter(timeout ?? DefaultRenderTimeout);
		return linked;
	}
}
