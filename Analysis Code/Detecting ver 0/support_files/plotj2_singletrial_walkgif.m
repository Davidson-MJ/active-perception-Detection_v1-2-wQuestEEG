% plotj2_singletrial_walkgif.m

% create a frame by frame gif of  a single walking trial.

%% Next: try
% VideoWriter and example
%here: https://au.mathworks.com/matlabcentral/answers/271490-create-a-movie-from-images

cd([datadir filesep 'ProcessedData'])

pfols = dir([pwd filesep '*summary_data.mat']);
nsubs= length(pfols);
%%
%for ppant % AH

%
clf;
%
cd([datadir filesep 'ProcessedData'])
load(pfols(ippant).name, 'HeadPos');
%%
%extract mean Head Y data
ippant = 1;
itrial = 63;

HeadY= HeadPos(itrial).Y;
HeadX =HeadPos(itrial).X;
Times = HeadPos(itrial).times;
HeadSway = HeadPos(itrial).Z;

% create axes (landscape and portrait layouts).
figure(1);clf;
set(gcf, 'units', 'normalized', 'position', [0.35,  0.1, .6, .35],'color', 'w')
% create axes (landscape and portrait layouts).
figure(2);clf;
set(gcf, 'units', 'normalized', 'position', [0.1,  0.1, .25, .7],'color', 'w')
%
useD= {HeadY, HeadSway};
yHlabel= {'y-axis', 'z-axis'};
shg
% % hor and ver aligned

axare= {'vertical', 'roll'};
ysare = {'height', 'sway'};
mrkers = {'_', 'o'};
pksare = {HeadPos(itrial).gaitData.peak};
ylimsare = [1.4 1.55; -.1 .1];
loc_troughs = HeadPos(itrial).Y_gait_troughs;
for iax=1:2
    
    figure(iax)
    if iax==1
        plot(Times, useD{iax}, 'color', [.7 .7 .7]);
        xlabel('Trial time [s]');
        ylabel({[ysare{iax} ' [m]']})
        
        ylim(ylimsare(iax,:))
        xlim([Times(1) Times(end)])
        set(gca,'fontsize', 20, 'YTick', [ylimsare(iax,1):.1:ylimsare(iax,2)])
        text(0.1, ylimsare(iax,2), ['Head \bf' ysare{iax} '\rm' ], 'VerticalAlignment', 'bottom',...
            'fontsize', 20)
    else
        %change order for sway plot:
        plot(useD{iax}, Times, 'color', [.7 .7 .7]);
        
        xlabel({[ysare{iax} ' [m]']})
        ylabel('Trial time [s]');
        
        xlim(ylimsare(iax,:))
        ylim([Times(1) Times(end)])
        set(gca,'fontsize', 20, 'XTick', [ylimsare(iax,1):.1:ylimsare(iax,2)])
            text(ylimsare(iax,1), Times(end), ['Head \bf' ysare{iax} '\rm' ], 'VerticalAlignment', 'bottom',...
                'fontsize', 20)
    end
    
end

%%
%
% use this as the first frame of the gif.
cd([datadir filesep 'Figures'])
tsteps = (mean(diff(Times)));

%
% which data points in times, correspond to 20Hz points?
plotAt =0:1/20:Times(end);
plotAtidx = dsearchn(Times, plotAt');

% keep track of which steps to plot.
stepcounter=2;
nextstep = loc_troughs(stepcounter);
%

for iax=2%:2 % move the ax to the outside for loop, so one gif per axis.
    
    figure(iax)
    hold on;
    gif(['myWalk_' ysare{iax} '.gif'],'DelayTime',1/20,'LoopCount',1,'frame',gcf, 'overwrite', true) % 20 Hz replay
    
     for iframe = plotAtidx'
    %% update both subplots
    
        tmpD = useD{iax}';
        hold on
        if iax==1
            plot(Times(1:iframe), tmpD(1:iframe), 'color', 'k', 'linew', 2);
        else
            plot(tmpD(1:iframe),Times(1:iframe),  'color', 'k', 'linew', 2);
        end
        
        set(gca,'fontsize', 20)
        if iax==1
            %%
        t=title(['\rmTrial time: ' sprintf('%.2f',Times(iframe)) 's'],...
            'fontsize', 20, 'units', 'normalized',...
            'position', [.75, 1.01, 0]);
        
        end
        hold on
        %add the step data and colour if we have reached it:
        if iframe >=nextstep
            
            %use which coour?
            if strcmp(pksare{stepcounter}(2), 'L')
                usecol='b';   
                offset=1;
                swayoffset= .01;
                swayalign ='Right';
            else
                usecol='r';  
                offset=2;
                swayoffset= -.01;
                swayalign ='Left';
            end
            
            LRtextpos =ylimsare(1,1) + .1 * diff(ylimsare(1,:));
            xData= Times(loc_troughs(stepcounter));
            yData=useD{iax}(loc_troughs(stepcounter));
            
            if iax==1
               p= plot(xData,yData, 'color', [usecol, .2], 'marker', mrkers{iax}, ...
                    'linestyle', 'none', 'markersize', 20, 'linewidth', 2);%, 'alpha', .2);
                
                text(Times(loc_troughs(stepcounter)), LRtextpos, pksare{stepcounter}(2), ...
                    'HorizontalAlignment', 'center','fontsize',20, 'Color', usecol)
            else % switch order
                
                plot(0, xData, 'color', [usecol, .2], 'marker', mrkers{iax}, ...
                    'linestyle', 'none', 'markersize', 20, 'linewidth', 2);
                %offset character from data.
                xpos= useD{iax}(loc_troughs(stepcounter)) + -.1;%swayoffset;
                
                text( -.07, Times(loc_troughs(stepcounter)),pksare{stepcounter}(2),...
                    'HorizontalAlignment', swayalign,'fontsize',20, 'Color', usecol)
            end
            
            %keep track of steps elapsed.
            
                stepcounter=stepcounter+1;
                nextstep = loc_troughs(stepcounter);
            
        end
        
      gif; % write to gif file.   
     end
    
    
   
    
end

%%
gifinfo = imfinfo('myfile.gif');
DelayTimes = [gifinfo.DelayTime] %% can't get the delay time less than 1!
%%
% change_gif_delay('myfile.gif', .001, 'myfile2.gif') % arg 1 in sec.