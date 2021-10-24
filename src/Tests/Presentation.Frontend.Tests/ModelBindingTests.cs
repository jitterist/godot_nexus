using System;
using System.Collections.Generic;
using App.Utilities;
using Microsoft.FSharp.Control;
using NUnit.Framework;

namespace App.Presentation.Frontend.Tests
{
	using IChangeEvent = IEvent<FSharpHandler<IReadOnlyList<string>>, IReadOnlyList<string>>;
	using ChangeEvent = FSharpEvent<FSharpHandler<IReadOnlyList<string>>, IReadOnlyList<string>>;

	[TestFixture]
	public class ModelBindingTests
	{
		private const string InitialModelText = "Initial Model Text";

#pragma warning disable CS8618
		private TestModel model;
		private TestFrontend frontend;
		private ModelBinding<TestModel, TestFrontend> sut;
#pragma warning restore CS8618

		[SetUp]
		public void SetUp()
		{
			model = new();
			frontend = new();
			sut = ModelBinding.New(model, frontend);
		}

		[Test]
		public void Binding_Updates_Frontend_Upon_Construction()
		{
			Assert.That(frontend.Text, Is.EqualTo(InitialModelText));
		}

		[Test]
		public void Frontend_Change_Is_Propagated()
		{
			frontend.Text = "Changed from frontend!";
			Assert.That(model.Text, Is.EqualTo("Changed from frontend!"));
		}

		[Test]
		public void Model_Change_Is_Propagated()
		{
			model.Text = "Changed from backend!";
			Assert.That(frontend.Text, Is.EqualTo("Changed from backend!"));
		}

		[Test]
		public void Disposing_Binding_Disposes_Frontend_And_Model()
		{
			var frontendWasDisposed = false;
			var modelWasDisposed = false;
			frontend.Disposed += delegate { frontendWasDisposed = true; };
			model.Disposed += delegate { modelWasDisposed = true; };

			sut.Dispose();

			Assert.That(frontendWasDisposed);
			Assert.That(modelWasDisposed);
		}

		[Test]
		public void After_Dispose_Frontend_Connections_Are_Cut()
		{
			sut.Dispose();
			frontend.Text = "No change!";

			Assert.That(frontend.Text, Is.EqualTo(InitialModelText));
			Assert.That(model.Text, Is.EqualTo(InitialModelText));
		}

		[Test]
		public void After_Dispose_Backend_Connections_Are_Cut()
		{
			sut.Dispose();
			model.Text = "No change!";

			Assert.That(frontend.Text, Is.EqualTo(InitialModelText));
			Assert.That(model.Text, Is.EqualTo("No change!"));
		}

		[Test]
		public void Dispose_Works_Once()
		{
			var frontendDisposedCount = 0;
			var modelDisposedCount = 0;
			frontend.Disposed += delegate { frontendDisposedCount++; };
			model.Disposed += delegate { modelDisposedCount++; };

			sut.Dispose();
			sut.Dispose();

			Assert.That(frontendDisposedCount, Is.EqualTo(1));
			Assert.That(modelDisposedCount, Is.EqualTo(1));
		}


		private class TestModel : IPropertyChanged, IDisposable
		{
			internal string Text
			{
				get => text;
				set
				{
					text = value;
					propertyChanged.Trigger(this, new[] {nameof(Text)});
				}
			}

			public IChangeEvent PropertyChanged => propertyChanged.Publish;

			internal event EventHandler? Disposed;

			private string text = InitialModelText;
			private readonly ChangeEvent propertyChanged = new();

			public void Dispose() => Disposed?.Invoke(this, EventArgs.Empty);
		}

		private class TestFrontend : IFrontend<TestModel>
		{
			internal string Text
			{
				get => text;
				set => ModelUpdateRequired?.Invoke(x => x.Text = value);
			}

			private string text = "";

			public event EventHandler? Disposed;
			public event Action<Action<TestModel>>? ModelUpdateRequired;

			public void NotifyModelUpdated(TestModel model) => text = model.Text;
			public void Dispose() => Disposed?.Invoke(this, EventArgs.Empty);
		}
	}
}
