using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineInsides
{
    enum Direction
    {
        Still,
        Left,
        Right
    }

    class LinkedTape
    {
        public int? key;
        public int position;
        public LinkedTape previous;
        public LinkedTape next;

        private LinkedTape(){}
        private LinkedTape(int? key, int position, LinkedTape previous = null, LinkedTape next = null)
        {
            this.key = key;
            this.position = position;
            this.previous = previous;
            this.next = next;
        }
        static public LinkedTape Create(int?[] keys)
        {
            LinkedTape tape = new LinkedTape(keys[0], 0);
            LinkedTape save = tape;
            for (int loop = 1; loop < keys.Length; loop++)
            {
                tape.next = new LinkedTape(keys[loop], loop, tape);
                tape = tape.next;
            }
            return save;
        }

        public int? Peek( int position)
        {
            if (this.position == position)
                return this.key;

            LinkedTape tape = this;
            if (this.position > position)
                for (int loop = this.position; loop > position; loop--)
                {
                    if (tape.previous == null)
                        return null;
                   tape = tape.previous;
                }
            else
                for (int loop = this.position; loop < position; loop++)
                {
                    if (tape.next == null)
                        return  null;
                    tape = tape.next;
                }
            return tape.key;
        }
        public LinkedTape Write(int? value, int position)
        {
            if (this.position == position)
            {
                this.key = value;
                return this;
            }
            LinkedTape tape = this;
            if (this.position > position)
                for (int loop = this.position; loop > position; loop--)
                {
                    if (tape.previous == null)
                        tape.previous = new LinkedTape(value, tape.position-1, null, tape);
                   tape = tape.previous;
                }
            else
                for (int loop = this.position; loop < position; loop++)
                {
                    if (tape.next == null)
                        tape.next = new LinkedTape(value, tape.position+1, tape, null);
                    tape = tape.next;
                }
            tape.key = value;
            return tape;

        }
        public override string ToString()
        {
 	        return String.Format("Tape[{0}]: {1}. P:{2};N:{3};", position, key, (previous == null)? "null": "Tape", (next == null)? "null": "Tape");
        }
    }
    class MachineTape
    {
        public LinkedTape tape;
        public int headPosition;

        public MachineTape(int?[] keys, int headPosition)
        {
            tape = LinkedTape.Create(keys);
            this.headPosition = headPosition;
        }
        public MachineTape(MachineTape clone)
        {
            tape = clone.tape;
            headPosition = clone.headPosition;
        }

        public int? Peek(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left: return tape.Peek(headPosition-1);
                case Direction.Right: return tape.Peek(headPosition+1);
                case Direction.Still: return tape.Peek(headPosition); 
                default: return null;
            }
        }
        public void WriteAndMove(int? value, Direction direction)
        {
            tape = tape.Write(value, headPosition);
            switch (direction)
            {
                case Direction.Left: headPosition--; break;
                case Direction.Right: headPosition++; break;
                case Direction.Still: break;
            }
            if (tape.Peek(headPosition) == null)
                tape = tape.Write(null, headPosition);
            if (headPosition == -1)
            {
                while (tape.next != null)
                {
                    tape.position++;
                    tape = tape.next;
                }
                tape.position++;
                headPosition++;
            }
        }
    }

    [Serializable]
    class MachineProgram
    {
        //curState, curKey : futState, futKey, direction 
        public readonly Dictionary<Tuple<int, int?> ,Tuple<int, int?, Direction>> program;

        public MachineProgram(string[] values)
        {
            program = new Dictionary<Tuple<int, int?>, Tuple<int, int?, Direction>>();
            foreach(string value in values)
            {
                string[] split = value.Split(new char[] {';'});
                int currentState = Int32.Parse(split[0].Substring(1));
                int? currentKey = (String.IsNullOrWhiteSpace(split[1])) ? null : (int?)Int32.Parse(split[1]);
                int nextState = Int32.Parse(split[2].Substring(1));
                int? nextKey = (String.IsNullOrWhiteSpace(split[3]))? null : (int?)Int32.Parse(split[3]);
                Direction movingDirection = Direction.Still;
                switch (split[4].ToUpperInvariant())
                {
                    case "L": movingDirection = Direction.Left; break;
                    case "R": movingDirection = Direction.Right; break;
                    case "C": movingDirection = Direction.Still; break;
                }
                program[new Tuple<int, int?>(currentState, currentKey)] = new Tuple<int, int?, Direction>(nextState, nextKey, movingDirection);
            }
        }

        public void SaveToFile(string filename)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                formatter.Serialize(stream, this);
        }
        public static MachineProgram LoadFromFile(string filename)
        {
            MachineProgram program = null;
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                program = (MachineProgram)formatter.Deserialize(stream);
            return program;
        }
    }
}
namespace TuringMachine
{
    class TurMach : INotifyPropertyChanged
    {
        private int state = 1;
        public int State{ get {return state;} set { SetField(ref state, value);} }

        private MachineInsides.MachineTape tape;
        public MachineInsides.MachineTape Tape { get {return tape;} set { SetField(ref tape, value); }}

        private string log = "0 - Start";
        public string Log{ get {return log;} set { SetField(ref log, value);} }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public MachineInsides.MachineProgram program;
        int logLoop = 0;
        bool isRunning = false;
        public int runningDelay = 100;

        public TurMach() {}
        public TurMach(int?[] keys, int headPosition, string[] commands)
        {
            Tape = new MachineInsides.MachineTape(keys, headPosition);
            program = new MachineInsides.MachineProgram(commands);
        }
        public TurMach(int?[] keys, int headPosition, MachineInsides.MachineProgram program)
        {
            Tape = new MachineInsides.MachineTape(keys, headPosition);
            this.program = program;
        }

        public void StartExecution()
        {
            isRunning = true;
            while (isRunning)
            {
                if (DoStep())
                    return;
                Thread.Sleep(runningDelay);
            }
        }
        public void StopExecution()
        {
            isRunning = false;
        }
        public bool DoStep()
        {
            int? curKey = tape.Peek(MachineInsides.Direction.Still);
            var keyTuple = new Tuple<int, int?>(State, curKey);
            var futureValuesTuple = program.program[keyTuple];
            State = futureValuesTuple.Item1;
            tape.WriteAndMove(futureValuesTuple.Item2, futureValuesTuple.Item3);
            Tape = new MachineInsides.MachineTape(tape);
            int? selectedCell = tape.Peek(MachineInsides.Direction.Still);
            Log += String.Format("{0}{1} - State: {2}; Selected Cell:{3};", Environment.NewLine, ++logLoop, state.ToString(), (selectedCell == null)? "(Blank)": selectedCell.ToString());
            return (state == 0 || logLoop == 100);
        }
    }
}
