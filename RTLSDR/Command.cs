using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    public class Command
    {
        private CommandsEnum _command { get; set; }
        private byte[] _arguments { get; set; }

        public Command(CommandsEnum command, byte[] arguments)
        {
            _command = command;
            _arguments = arguments;
        }

        public Command(CommandsEnum command, int intArgument)
        {
            _command = command;

            byte[] arguments = new byte[4];

            arguments[0] = (byte)((intArgument >> 24) & 0xff);
            arguments[1] = (byte)((intArgument >> 16) & 0xff);
            arguments[2] = (byte)((intArgument >> 8) & 0xff);
            arguments[3] = (byte)(intArgument & 0xff);

            _arguments = arguments;
        }

        public Command(CommandsEnum command, short arg1, short arg2)
        {
            _command = command;

            byte[] arguments = new byte[4];

            arguments[0] = (byte)((arg1 >> 8) & 0xff);
            arguments[1] = (byte)(arg1 & 0xff);
            arguments[2] = (byte)((arg2 >> 8) & 0xff);
            arguments[3] = (byte)(arg2 & 0xff);

            _arguments = arguments;
        }

        public override string ToString()
        {
            return _command.ToString();
        }

        public byte[] ToByteArray()
        {
            var res = new List<byte>();
            res.Add((byte)_command);

            var arArray = new byte[4];
            for (var i=0; i < 4; i++)
            {
                arArray[i] = _arguments == null || i > _arguments.Length
                    ? (byte)0
                    : _arguments[i];
            }

            res.AddRange(arArray);

            return res.ToArray();
        }
    }
}
