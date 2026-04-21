namespace GVisionWpf.Exceptions
{
    public class PrsImageDequeueException : GVisionException
    {
        public PrsImageDequeueException() : base("Failed to dequeue from the PRS image queue.")
        {
            ErrorCode = "PRS_DEQUEUE_FAILED";
            TroubleShooting = new List<string>
            {
                "Ensure the queue is not empty before calling Dequeue.",
                "Check if another thread is modifying the queue concurrently.",
                "Verify the queue initialization and data enqueue process."
            };
        }
    }
}