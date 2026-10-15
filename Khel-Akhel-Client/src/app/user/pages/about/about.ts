import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface StudioDiscipline {
  code: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './about.html',
  styleUrl: './about.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class About {
  readonly studioDisciplines: StudioDiscipline[] = [
    {
      code: '01',
      title: 'Art & Creative Practice',
      description:
        'Visual art, conceptual art, illustration, calligraphy, sculpture, performance, photography, sound, and poetry.',
    },
    {
      code: '02',
      title: 'Architecture & Spatial Design',
      description:
        'Architecture-related design, interiors, spatial design, scenography, exhibitions, environments, and public spaces.',
    },
    {
      code: '03',
      title: 'Craft, Material Culture & Objects',
      description:
        'Craft, artisan collaborations, bespoke furniture, collectible lifestyle objects, and material experimentation.',
    },
    {
      code: '04',
      title: 'Cultural, Festival & Public Experiences',
      description:
        'Festivals, curated exhibitions, cultural programming, public art interventions, and immersive environments.',
    },
    {
      code: '05',
      title: 'Film, Photography, Sound & Media',
      description:
        'Documentary film, narrative photography, audiovisual installations, soundscapes, and digital media.',
    },
    {
      code: '06',
      title: 'Learning, Play & Facilitation',
      description:
        'Creative workshops, alternative learning spaces, immersive play experiments, and reflective circles.',
    },
    {
      code: '07',
      title: 'Research, Documentation & Knowledge',
      description:
        'Design research, cultural and ecological archives, craft methodologies, and editorial publications.',
    },
    {
      code: '08',
      title: 'Community, Ecology & Cultural Initiatives',
      description:
        'Participatory community projects, ecological creativity, cultural preservation, and regenerative practices.',
    },
    {
      code: '09',
      title: 'Creative Consultancy & Direction',
      description:
        'Creative art direction, spatial strategy, curation, experience design, and visionary project development.',
    },
    {
      code: '10',
      title: 'Publishing & Editorial Content',
      description:
        'Art monographs, publications, digital narratives, educational material, and visual archives.',
    },
    {
      code: '11',
      title: 'Commercial Products & Licensing',
      description:
        'Distribution, manufacture, and licensing of creative works, limited edition design objects, and apparel.',
    },
    {
      code: '12',
      title: 'Collaborations & Joint Ventures',
      description:
        'Cross-disciplinary partnerships, creative commissions, and co-curated cultural projects worldwide.',
    },
  ];
}
