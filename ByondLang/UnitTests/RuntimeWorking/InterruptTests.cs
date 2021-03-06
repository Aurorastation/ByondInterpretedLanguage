﻿using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ByondLang.UnitTests.RuntimeWorking
{
    public class InterruptTests
    {
        [Fact]
        public async void SimpleWhileTrue()
        {
            using (var runtime = new Runtime(null))
            {
                using (var program = await runtime.BuildContext((r, c, m) => new TestProgram(r, c, m)))
                {
                    var result = program.ExecuteScript("while(true) {}");

                    Assert.True(result.Wait(5000));
                    Assert.False(program.HasErrored);
                }
            }
        }

        [Fact]
        public async void NativeFucntionWhileTrue()
        {
            using (var runtime = new Runtime(null))
            {
                using (var program = await runtime.BuildContext((r, c, m) => new TestProgram(r, c, m)))
                {
                    var result = program.ExecuteScript("while(true) {atob('QQ==')}");

                    Assert.True(result.Wait(5000));
                    Assert.False(program.HasErrored);
                }
            }
        }
    }
}
