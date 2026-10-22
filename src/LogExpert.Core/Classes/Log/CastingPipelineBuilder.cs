using System.Collections.Concurrent;

using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log;

public class CastingPipelineBuilder : IPipeline<object, object>
{
    private readonly List<Func<object, object>> _pipelineSteps = [];
    private BlockingCollection<object>[] _buffers;


    public event Action<object> Finished;

    public void AddStep (Func<object, object> stepFunc)
    {
        _pipelineSteps.Add(stepFunc);
    }

    public void Execute (object input)
    {
        BlockingCollection<object> first = _buffers[0];
        first.Add(input);
    }

    public void Complete ()
    {
        if (_buffers.Length > 0)
        {
            _buffers[0].CompleteAdding();
        }
    }

    public IPipeline<object, object> GetPipeline ()
    {
        //Create Buffers
        _buffers = _pipelineSteps.Select(step => new BlockingCollection<object>()).ToArray();

        int bufferIndex = 0;
        foreach (Func<object, object> pipelineStep in _pipelineSteps)
        {
            var bufferIndexLocal = bufferIndex; //with this remains same in each thread
            _ = Task.Run(() =>
            {
                //GetConsuminEnumerable => is blocking when the collection is empty
                foreach (object input in _buffers[bufferIndexLocal].GetConsumingEnumerable().Select(pipelineStep))
                {
                    bool isLastStep = bufferIndexLocal == _pipelineSteps.Count - 1;
                    if (isLastStep)
                    {
                        //BeginInvoke would be better https://stackoverflow.com/questions/1916095/how-do-i-make-an-eventhandler-run-asynchronously/16336361#16336361
                        Finished?.Invoke(input);
                    }
                    else
                    {
                        BlockingCollection<object> next = _buffers[bufferIndexLocal + 1];
                        next.Add(input);
                    }
                }
            });

            bufferIndex++;
        }

        return this;
    }
}
