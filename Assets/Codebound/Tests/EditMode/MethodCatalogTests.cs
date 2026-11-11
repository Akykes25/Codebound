using Codebound.Levels;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class MethodCatalogTests
    {
        [Test]
        public void EachMethodShowsHowToWriteItAndWhatItDoes()
        {
            var entries = new[]
            {
                new ActionEntry("MoveRight", "MoveRight();", "Mueve al robot una celda a la derecha."),
                new ActionEntry("ChargeCore", "ChargeCore(int carga);", "Deja el núcleo con esa carga."),
            };

            Assert.That(MethodCatalogText.Format(entries), Is.EqualTo(
                "MoveRight();\nMueve al robot una celda a la derecha.\n\n" +
                "ChargeCore(int carga);\nDeja el núcleo con esa carga."));
        }

        [Test]
        public void ALevelWithoutMethodsShowsNothing()
        {
            Assert.That(MethodCatalogText.Format(new ActionEntry[0]), Is.Empty);
        }

        [Test]
        public void TheCatalogOnlyShowsTheMethodsOfTheLevel()
        {
            var catalog = LoadCatalog();

            var level1 = MethodCatalogText.Format(catalog.For(LoadLevel(1)));
            var level4 = MethodCatalogText.Format(catalog.For(LoadLevel(4)));
            var level5 = MethodCatalogText.Format(catalog.For(LoadLevel(5)));

            Assert.That(level1, Does.Contain("MoveRight();").And.Contain("Interact();").And.Not.Contain("ChargeCore"));
            Assert.That(level4, Does.Contain("ChargeCore(int carga);").And.Not.Contain("HasKey"));
            Assert.That(level5, Does.Contain("HasKey()").And.Contain("CollectKey();").And.Contain("OpenGate();"));
        }
    }
}
