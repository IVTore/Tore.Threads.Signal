/*————————————————————————————————————————————————————————————————————————————
    ————————————————————————————————————————————————————
    |   Signal : C# Thread signaling class library.    |
    ————————————————————————————————————————————————————

© Copyright 2023 İhsan Volkan Töre.

Author              : IVT.  (İhsan Volkan Töre)
Version             : 202304060942 (v1.0.0).
License             : MIT.

History             :
202304060942: IVT   : First version.
————————————————————————————————————————————————————————————————————————————*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tore.Threads {

    /**———————————————————————————————————————————————————————————————————————————
        CLASS:  Signal.                                                 <summary>
        USAGE:                                                          <br/>
            Signal generates a waitable and cancellable object          <br/>
            for asynchronous communications among threads.              <para/>
            It can use an external CancellationTokenSource otherwise    <br/>
            it builds an internal one.                                  <para/>
            Assume there exists threads A and B in which A would        <br/>
            wait for an operation in B and they share a Signal object:  <para/>
            If A issues an await signal.Wait() it will wait until       <br/>
            B calls signal.EndWait() or signal.Cancel().                <para/>
            After signal.EndWait() it can be reused by signal.Wait().   <br/>
            After signal.Cancel() reuse is not possible.                <para/>
            Dispose after use.                                          </summary>
    ————————————————————————————————————————————————————————————————————————————*/
    public class Signal : IDisposable {

        #region Privates.
        /*————————————————————————————————————————————————————————————————————————————
            ————————————————————
            |   Properties.    |
            ————————————————————
        ————————————————————————————————————————————————————————————————————————————*/

        // Token source for wait signaling.
        private CancellationTokenSource waiterSource = new();

        // Token source for linking wait and cancellation.
        private CancellationTokenSource linkedSource;
        
        // Token for wait signaling.
        private CancellationToken waiter => waiterSource.Token;
        
        // Linked Token for wait and cancellation.
        private CancellationToken linked => linkedSource.Token;
        
        // Lock for token sources.
        private object tokenLock = new();

        #endregion

        #region Properties.
        /*————————————————————————————————————————————————————————————————————————————
            ————————————————————
            |   Properties.    |
            ————————————————————
        ————————————————————————————————————————————————————————————————————————————*/

        /**———————————————————————————————————————————————————————————————————————————
          PROP: cancelSourceInternal: bool.                                 <summary>
          GET : Returns if cancellation token source is created by          <br/>
                signal object or not                                        </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public bool cancelSourceInternal { get; } = false;

        /**———————————————————————————————————————————————————————————————————————————
          PROP: cancelSourceInternal: CancellationTokenSource.              <summary>
          GET : Returns the cancellation token source                       </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public CancellationTokenSource cancelSource { get; private set; }

        /**———————————————————————————————————————————————————————————————————————————
          PROP: cancelToken: CancellationToken.                             <summary>
          GET : Returns the cancellation token.                             </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public CancellationToken cancelToken => cancelSource.Token;

        /**———————————————————————————————————————————————————————————————————————————
          PROP: isCancelled: bool.                                          <summary>
          GET : Returns if cancellation token is cancelled or not.          </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public bool isCancelled => cancelSource.Token.IsCancellationRequested;

        #endregion

        /**——————————————————————————————————————————————————————————————————————————
          CTOR: Signal                                                  <summary>
          TASK:                                                         <br/>
                Constructs a Signal object.                             <para/>
          ARGS:                                                         <br/>
                cancellationTokenSource : CancellationTokenSource   :
                    An external CancellationTokenSource if required.    <br/>
                    If null this is built by the object.                </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public Signal(CancellationTokenSource? cancellationTokenSource = null) {
            cancelSourceInternal = (cancellationTokenSource == null);
            cancelSource = cancellationTokenSource ?? new();
            if (isCancelled){   // When an external source given with cancelled token.
                Dispose();
                ThrowIfCancelled();
            }
            linkedSource = CancellationTokenSource
                          .CreateLinkedTokenSource(cancelToken, waiter);
        }

        /**——————————————————————————————————————————————————————————————————————————
          DTOR: Signal.                                                     <summary>
          TASK: Disposes the Signal object.                                 </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        ~Signal() { 
            Dispose();
        }

        /**<inheritdoc/>*/
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**<inheritdoc/>*/
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                linkedSource?.Dispose();
                waiterSource?.Dispose();
                if (cancelSourceInternal)
                    cancelSource?.Dispose();
            }
        }

        /**———————————————————————————————————————————————————————————————————————————
          FUNC: Wait.                                                       <summary>
          TASK:                                                             <br/>
            Waits until EndWait() or Cancel() is called by another thread.  <para/>
          RETV:     : Task   : awaited task.                                <para/>
          INFO: Unless cancelled, a signal can be waited multiple times.    </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        async public Task Wait() {
            ResetWaiterSources();
            try { 
                while (!linked.IsCancellationRequested)
                    await Task.Delay(10000, linked);
            } catch (TaskCanceledException) {
                ;                                       // Do nothing.
            } catch (ObjectDisposedException) {
                throw;                                  // Almost rhetorical.
            }
        }

        /**———————————————————————————————————————————————————————————————————————————
          FUNC: EndWait.                                                     <summary>
          TASK: Terminates an await Wait() started from another thread.        <para/>
          INFO: After EndWait() signal can be reused again.        </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public void EndWait() {
            lock(tokenLock)
                waiterSource.Cancel();
        }

        /**———————————————————————————————————————————————————————————————————————————
          FUNC: Cancel .                                                     <summary>
          TASK: Terminates an await Wait() started from another thread.          <br/>
                If any associated threads use the cancellationToken of signal    <br/>
                they will receive a cancellation request also.                 <para/>
          INFO: After Cancel(), signal can not be reused.                   </summary>
        ————————————————————————————————————————————————————————————————————————————*/
        public void Cancel() {
            cancelSource.Cancel();
        }
        
        private void ThrowIfCancelled(){
            if (isCancelled)
                throw new TaskCanceledException("Signal");
        }

        private void ResetWaiterSources() {
            ThrowIfCancelled();
            if (!linked.IsCancellationRequested)
                return;
            lock(tokenLock){ 
                waiterSource.Dispose();
                linkedSource.Dispose();
                waiterSource = new();
                linkedSource = CancellationTokenSource
                              .CreateLinkedTokenSource(cancelToken, waiter);
            }
        }
    }

}
