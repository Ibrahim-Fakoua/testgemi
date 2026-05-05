The signals directory is for, well, signals. 

# For C\#
When emitting a signal, you do `Controller.Instance.Emit(new SimulationSignal())`
Where `SimulationSignal` is any signal inheriting from it. Never emit it directly, only it's children.

To listen to emits of signals, simply do `Controller.Instance.Subscribe<SimulationSignal>(MethodName)`
Where `MethodName` is defined somewhere in the script, publicly.

# For GDScript 
To emit a signal, you must construct the signal extending `SimulationSignal`.
Create the event using `var event = new SimulationSignal()`.
Where `SimulationSignal` is also never emitted directly, only it's children.

To subscribe, do `Controller.GDSubscribe("SimulationSignal", _method_name)`.
Where, again, `SimulationSignal` is never used directly.
