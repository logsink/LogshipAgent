using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Linux.JournalCtl
{
    internal class JournalHandle : SafeHandle
    {
        private JournalHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        public static JournalHandle Open(int flags)
        {
            var handle = new JournalHandle();
            int result = Interop.sd_journal_open(out IntPtr journal, flags);
            if (result < 0)
            {
                Interop.Throw(result, "Couldn't open journal file");
            }

            handle.SetHandle(journal);
            return handle;
        }

        protected override bool ReleaseHandle()
        {
            if (this.handle != IntPtr.Zero)
            {
                _ = Interop.sd_journal_close(this.handle);
            }

            return true;
        }
    }
}
