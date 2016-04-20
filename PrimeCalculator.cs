using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.SimpleMemory;

namespace Hast.Samples.Psc
{
    public class PrimeCalculator
    {
        public const int IsPrimeNumber_InputUInt32Index = 0;
        public const int IsPrimeNumber_OutputBooleanIndex = 0;
        public const int ArePrimeNumbers_InputUInt32CountIndex = 0;
        public const int ArePrimeNumbers_InputUInt32sStartIndex = 1;
        public const int ArePrimeNumbers_OutputUInt32sStartIndex = 1;

    
        public virtual void IsPrimeNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(IsPrimeNumber_InputUInt32Index);
            memory.WriteBoolean(IsPrimeNumber_OutputBooleanIndex, IsPrimeNumber(number));
        }

        public virtual void ArePrimeNumbers(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(ArePrimeNumbers_InputUInt32CountIndex);

            for (int i = 0; i < numberCount; i++)
            {
                uint number = memory.ReadUInt32(ArePrimeNumbers_InputUInt32sStartIndex + i);
                memory.WriteBoolean(ArePrimeNumbers_OutputUInt32sStartIndex + i, IsPrimeNumber(number));
            }
        }


        private bool IsPrimeNumber(uint number)
        {
            uint factor = number / 2;

            for (uint i = 2; i <= factor; i++)
            {
                if ((number % i) == 0) return false;
            }

            return true;
        }
    }


    public static class PrimeCalculatorExtensions
    {
        public static bool IsPrimeNumber(this PrimeCalculator primeCalculator, uint number)
        {
            var memory = new SimpleMemory(1);
            memory.WriteUInt32(PrimeCalculator.IsPrimeNumber_InputUInt32Index, number);
            primeCalculator.IsPrimeNumber(memory);
            return memory.ReadBoolean(PrimeCalculator.IsPrimeNumber_OutputBooleanIndex);
        }

        public static bool[] ArePrimeNumbers(this PrimeCalculator primeCalculator, uint[] numbers)
        {
            var memory = new SimpleMemory(numbers.Length + 1);

            memory.WriteUInt32(PrimeCalculator.ArePrimeNumbers_InputUInt32CountIndex, (uint)numbers.Length);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteUInt32(PrimeCalculator.ArePrimeNumbers_InputUInt32sStartIndex + i, numbers[i]);
            }

            primeCalculator.ArePrimeNumbers(memory);


            var output = new bool[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                output[i] = memory.ReadBoolean(PrimeCalculator.ArePrimeNumbers_OutputUInt32sStartIndex + i);
            }
            return output;
        }
    }
}
