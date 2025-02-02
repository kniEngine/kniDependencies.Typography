﻿//MIT, 2016-present, WinterDev
// some code from icu-project
// © 2016 and later: Unicode, Inc. and others.
// License & terms of use: http://www.unicode.org/copyright.html#License

using System;
using System.Collections.Generic;

namespace Typography.TextBreak
{
    public class CustomBreaker
    {
        //default for latin breaking engine
        readonly EngBreakingEngine _engBreakingEngine = new EngBreakingEngine();
        //current lang breaking engine
        BreakingEngine _breakingEngine;
        readonly List<BreakingEngine> _otherEngines = new List<BreakingEngine>();

        WordVisitor _visitor;
        int _endAt;
        bool _breakNumberAfterText;


        public CustomBreaker()
        {
            ThrowIfCharOutOfRange = false;
            _breakingEngine = _engBreakingEngine; //default eng-breaking engine
        }
        public void SetNewBreakHandler(NewWordBreakHandlerDelegate newWordBreakHandler)
        {
            _visitor = new DelegateBaseWordVisitor(newWordBreakHandler);
        }
        public WordVisitor CurrentVisitor
        {
            get => _visitor;
            set => _visitor = value;
        }

        //
        public EngBreakingEngine EngBreakingEngine => _engBreakingEngine;
        //
        public bool BreakNumberAfterText
        {
            get => _breakNumberAfterText;
            set
            {
                _breakNumberAfterText = value;
                _engBreakingEngine.BreakNumberAfterText = value;
                //TODO: apply to other engine
            }
        }
        public bool ThrowIfCharOutOfRange { get; set; }

        public void AddBreakingEngine(BreakingEngine engine)
        {
            //TODO: make this accept more than 1 engine
            _otherEngines.Add(engine);
            _breakingEngine = engine;
        }
        protected BreakingEngine SelectEngine(int c)
        {
            //from 
            InputReader.SeparateCodePoint(c, out char c0, out char c1);
            return SelectEngine(c0);
        }
        protected BreakingEngine SelectEngine(char c)
        {
            if (_breakingEngine.CanHandle(c))
            {
                return _breakingEngine;
            }
            else
            {
                //find other engine
                for (int i = _otherEngines.Count - 1; i >= 0; --i)
                {
                    //not the current engine 
                    //and can handle the character
                    BreakingEngine engine = _otherEngines[i];
                    if (engine != _breakingEngine && engine.CanHandle(c))
                    {
                        return engine;
                    }
                }

                //default 
#if DEBUG
                if (!_engBreakingEngine.CanHandle(c))
                {
                    //even default can't handle the char

                }
#endif
                return _engBreakingEngine;
            }
        }

        void BreakWords()
        {
            int startAt = _visitor.Offset;
            BreakingEngine currentEngine = _breakingEngine = (UseUnicodeRangeBreaker) ? _engBreakingEngine : SelectEngine(_visitor.C0);
            for (; ; )
            {
                //----------------------------------------
                currentEngine.BreakWord(_visitor); //please note that len is decreasing
                switch (_visitor.State)
                {
                    default: throw new OpenFont.OpenFontNotSupportedException();

                    case VisitorState.End:
                        //ok
                        return;
                    case VisitorState.OutOfRangeChar:
                        {
                            //find proper breaking engine for current char

                            BreakingEngine anotherEngine = SelectEngine(_visitor.Char);
                            if (anotherEngine == currentEngine)
                            {
#if DEBUG
                                //some time with error char[] buffer
                                //we may found high surrogate but not followed by low-surrogate
                                //or low without high etc
                                //we should handle this!
                                if (char.IsLowSurrogate(_visitor.Char))
                                {

                                }
#endif

                                if (ThrowIfCharOutOfRange) throw new OpenFont.OpenFontNotSupportedException($"A proper breaking engine for character '{_visitor.Char}' was not found.");
                                startAt = _visitor.Offset + 1;
                                _visitor.SetCurrentIndex(startAt);
                                _visitor.AddWordBreakAtCurrentIndex(WordKind.Unknown);

                            }
                            else
                            {
                                currentEngine = anotherEngine;
                                startAt = _visitor.Offset;

                            }
                        }
                        break;
                }
            }
        }
        
        
        public void BreakWords(int[] charBuff, int startAt, int len)
        {
            //conver to char buffer 
            int j = charBuff.Length;
            if (j < 1)
            {
                _endAt = 0;
                return;
            }
            _endAt = startAt + len;
            _visitor.LoadText(charBuff, startAt, len);

            //----------------------------------------
            //select breaking engine
            //int endAt = startAt + len;
            //InputReader reader = new InputReader(charBuff, startAt, endAt - startAt);
            BreakWords();

        }

        public void BreakWords(char[] charBuff, int startAt, int len)
        {
            //conver to char buffer 
            int j = charBuff.Length;
            if (j < 1)
            {
                _endAt = 0;
                return;
            }
            _endAt = startAt + len;
            _visitor.LoadText(charBuff, startAt, len);

            //----------------------------------------
            //select breaking engine  

            BreakWords();
        }

        public BreakingEngine GetBreakingEngineFor(char c)
        {
            return SelectEngine(c);
        }

        /// <summary>
        /// use unicode range breaker 
        /// </summary>
        public bool UseUnicodeRangeBreaker
        {
            get => _engBreakingEngine.EnableUnicodeRangeBreaker;
            set => _engBreakingEngine.EnableUnicodeRangeBreaker = value;
        }
    }

    public static class CustomBreakerExtensions
    {
        public static void BreakWords(this CustomBreaker breaker, string str)
        {
            char[] buffer = str.ToCharArray();
            breaker.BreakWords(buffer, 0, buffer.Length);
        }
        public static void BreakWords(this CustomBreaker breaker, char[] charBuff)
        {
            breaker.BreakWords(charBuff, 0, charBuff.Length);
        }
    }

}
