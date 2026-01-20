public interface IFailController
{
    void TryFail(ResultReason reason, string detail = null);
}
