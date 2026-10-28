namespace GReSym.Core.Exceptions;

public class EvaluationRunNotFoundException : DomainException
{
    public EvaluationRunNotFoundException(int runId)
        : base($"Evaluation run {runId} was not found.") { }
}
